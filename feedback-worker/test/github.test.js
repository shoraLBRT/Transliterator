import { test } from "node:test";
import assert from "node:assert/strict";
import { generateKeyPairSync, verify } from "node:crypto";

import { appJwt, privateKeyDer } from "../src/github.js";

const { privateKey, publicKey } = generateKeyPairSync("rsa", { modulusLength: 2048 });
const pkcs1Pem = privateKey.export({ type: "pkcs1", format: "pem" });
const pkcs8Pem = privateKey.export({ type: "pkcs8", format: "pem" });

function decode(part) {
    return JSON.parse(Buffer.from(part, "base64url").toString("utf8"));
}

/** GitHub выдаёт ключ приложения в PKCS#1, а Web Crypto читает только PKCS#8. */
test("a PKCS#1 key from GitHub is wrapped into the PKCS#8 that Web Crypto reads", () => {
    const expected = privateKey.export({ type: "pkcs8", format: "der" });

    assert.deepEqual(Buffer.from(privateKeyDer(pkcs1Pem)), expected);
});

test("a PKCS#8 key is taken as it is", () => {
    const expected = privateKey.export({ type: "pkcs8", format: "der" });

    assert.deepEqual(Buffer.from(privateKeyDer(pkcs8Pem)), expected);
});

test("a key pasted with literal \\n instead of line breaks is understood", () => {
    const oneLine = pkcs1Pem.trim().replace(/\n/g, "\\n");
    const expected = privateKey.export({ type: "pkcs8", format: "der" });

    assert.deepEqual(Buffer.from(privateKeyDer(oneLine)), expected);
});

for (const [name, pem] of [["PKCS#1", pkcs1Pem], ["PKCS#8", pkcs8Pem]]) {
    test(`the app JWT is signed by the app key (${name})`, async () => {
        const now = Date.UTC(2026, 9, 27, 12, 0, 0);
        const jwt = await appJwt(123, pem, now);
        const [header, payload, signature] = jwt.split(".");

        assert.deepEqual(decode(header), { alg: "RS256", typ: "JWT" });
        assert.deepEqual(decode(payload), { iat: now / 1000 - 60, exp: now / 1000 + 540, iss: "123" });

        const valid = verify("RSA-SHA256", Buffer.from(`${header}.${payload}`), publicKey, Buffer.from(signature, "base64url"));
        assert.ok(valid);
    });
}
