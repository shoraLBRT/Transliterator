import { test, beforeEach } from "node:test";
import assert from "node:assert/strict";
import { generateKeyPairSync } from "node:crypto";

import { handle, defuse, MAX_BODY_LENGTH, MAX_TITLE_LENGTH } from "../src/feedback.js";
import { resetTokenCache } from "../src/github.js";

const PAGE = "https://shoralbrt.github.io";

const { privateKey } = generateKeyPairSync("rsa", { modulusLength: 2048 });

const env = {
    ALLOWED_ORIGINS: `${PAGE}, http://localhost:5080`,
    GITHUB_REPOSITORY: "shoraLBRT/Transliterator",
    GITHUB_APP_ID: "123",
    GITHUB_APP_PRIVATE_KEY: privateKey.export({ type: "pkcs1", format: "pem" }),
    TURNSTILE_SECRET: "secret",
};

/** Подменный GitHub и Turnstile: записывает запросы и отвечает как они. */
function fakeServices({ captcha = true, issueStatus = 201 } = {}) {
    const calls = [];

    const fetchImpl = async (url, init = {}) => {
        calls.push({ url, init });

        if (url.includes("turnstile"))
            return Response.json({ success: captcha });
        if (url.endsWith("/installation"))
            return Response.json({ id: 42 });
        if (url.endsWith("/access_tokens"))
            return Response.json({ token: "installation-token", expires_at: new Date(Date.now() + 3600_000).toISOString() }, { status: 201 });
        if (url.endsWith("/issues"))
            return issueStatus === 201
                ? Response.json({ number: 7, html_url: "https://github.com/shoraLBRT/Transliterator/issues/7" }, { status: 201 })
                : new Response("{}", { status: issueStatus });

        throw new Error(`unexpected ${url}`);
    };

    return { calls, fetchImpl };
}

function post(data, origin = PAGE) {
    return new Request("https://feedback.example/", {
        method: "POST",
        headers: { Origin: origin, "Content-Type": "application/json", "CF-Connecting-IP": "203.0.113.5" },
        body: JSON.stringify(data),
    });
}

const message = { title: "Отзыв со страницы: Пропала хамза", body: "Пропала хамза", token: "turnstile-token" };

beforeEach(() => resetTokenCache());

test("a message from the page becomes an issue", async () => {
    const { calls, fetchImpl } = fakeServices();

    const response = await handle(post(message), env, fetchImpl);

    assert.equal(response.status, 201);
    assert.deepEqual(await response.json(), { number: 7, url: "https://github.com/shoraLBRT/Transliterator/issues/7" });
    assert.equal(response.headers.get("Access-Control-Allow-Origin"), PAGE);

    const issue = calls.find(call => call.url.endsWith("/repos/shoraLBRT/Transliterator/issues"));
    assert.deepEqual(JSON.parse(issue.init.body), { title: message.title, body: message.body });
    assert.equal(issue.init.headers.Authorization, "Bearer installation-token");
    assert.ok(issue.init.headers["User-Agent"]);
});

test("the turnstile token and the visitor address go to siteverify", async () => {
    const { calls, fetchImpl } = fakeServices();

    await handle(post(message), env, fetchImpl);

    const form = calls.find(call => call.url.includes("turnstile")).init.body;
    assert.equal(form.get("secret"), "secret");
    assert.equal(form.get("response"), "turnstile-token");
    assert.equal(form.get("remoteip"), "203.0.113.5");
});

test("a failed turnstile check creates nothing", async () => {
    const { calls, fetchImpl } = fakeServices({ captcha: false });

    const response = await handle(post(message), env, fetchImpl);

    assert.equal(response.status, 403);
    assert.deepEqual(await response.json(), { error: "captcha" });
    assert.ok(!calls.some(call => call.url.includes("api.github.com")));
});

test("another site gets no answer and no CORS headers", async () => {
    const { calls, fetchImpl } = fakeServices();

    const response = await handle(post(message, "https://evil.example"), env, fetchImpl);

    assert.equal(response.status, 403);
    assert.equal(response.headers.get("Access-Control-Allow-Origin"), null);
    assert.equal(calls.length, 0);
});

test("a request without an origin is not from a browser page", async () => {
    const { fetchImpl } = fakeServices();
    const request = new Request("https://feedback.example/", { method: "POST", body: JSON.stringify(message) });

    const response = await handle(request, env, fetchImpl);

    assert.equal(response.status, 403);
});

test("the preflight is answered for the page", async () => {
    const { calls, fetchImpl } = fakeServices();
    const request = new Request("https://feedback.example/", { method: "OPTIONS", headers: { Origin: "http://localhost:5080" } });

    const response = await handle(request, env, fetchImpl);

    assert.equal(response.status, 204);
    assert.equal(response.headers.get("Access-Control-Allow-Origin"), "http://localhost:5080");
    assert.match(response.headers.get("Access-Control-Allow-Headers"), /Content-Type/);
    assert.equal(calls.length, 0);
});

test("only POST creates", async () => {
    const { fetchImpl } = fakeServices();
    const request = new Request("https://feedback.example/", { method: "GET", headers: { Origin: PAGE } });

    const response = await handle(request, env, fetchImpl);

    assert.equal(response.status, 405);
});

for (const [name, data] of [
    ["empty title", { ...message, title: " " }],
    ["empty body", { ...message, body: "" }],
    ["missing token", { ...message, token: undefined }],
    ["title too long", { ...message, title: "а".repeat(MAX_TITLE_LENGTH + 1) }],
    ["body too long", { ...message, body: "а".repeat(MAX_BODY_LENGTH + 1) }],
    ["not a string", { ...message, body: 5 }],
]) {
    test(`bad input is rejected before any check: ${name}`, async () => {
        const { calls, fetchImpl } = fakeServices();

        const response = await handle(post(data), env, fetchImpl);

        assert.equal(response.status, 400);
        assert.equal(calls.length, 0);
    });
}

test("broken JSON is rejected", async () => {
    const { fetchImpl } = fakeServices();
    const request = new Request("https://feedback.example/", { method: "POST", headers: { Origin: PAGE }, body: "{" });

    const response = await handle(request, env, fetchImpl);

    assert.equal(response.status, 400);
});

test("a GitHub failure is reported, not hidden", async () => {
    const { fetchImpl } = fakeServices({ issueStatus: 500 });
    const quiet = console.error;
    console.error = () => {};

    try {
        const response = await handle(post(message), env, fetchImpl);

        assert.equal(response.status, 502);
        assert.equal(response.headers.get("Access-Control-Allow-Origin"), PAGE);
    } finally {
        console.error = quiet;
    }
});

test("the rate limiter stops a flood", async () => {
    const { calls, fetchImpl } = fakeServices();
    const keys = [];
    const limited = { ...env, FEEDBACK_LIMITER: { limit: async ({ key }) => (keys.push(key), { success: false }) } };

    const response = await handle(post(message), limited, fetchImpl);

    assert.equal(response.status, 429);
    assert.deepEqual(keys, ["203.0.113.5"]);
    assert.equal(calls.length, 0);
});

test("mentions are defused in the title and the body", async () => {
    const { calls, fetchImpl } = fakeServices();

    await handle(post({ ...message, title: "Отзыв: @octocat", body: "cc @shoraLBRT и @org/team, почта a@b" }), env, fetchImpl);

    const sent = JSON.parse(calls.find(call => call.url.endsWith("/issues")).init.body);
    assert.equal(sent.title, "Отзыв: @​octocat");
    assert.equal(sent.body, "cc @​shoraLBRT и @​org/team, почта a@​b");
});

test("a lone at sign is left as it is", () => {
    assert.equal(defuse("@ и @"), "@ и @");
});

test("the installation token is reused while it is fresh", async () => {
    const { calls, fetchImpl } = fakeServices();

    await handle(post(message), env, fetchImpl);
    await handle(post(message), env, fetchImpl);

    assert.equal(calls.filter(call => call.url.endsWith("/access_tokens")).length, 1);
    assert.equal(calls.filter(call => call.url.endsWith("/issues")).length, 2);
});
