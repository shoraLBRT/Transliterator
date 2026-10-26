// Доступ к GitHub от имени GitHub App: подписанный ключом приложения JWT
// меняется на токен установки, а токеном заводится issue. Ключ приложения
// живёт только в секретах функции и на страницу не попадает никогда.

const API = "https://api.github.com";

const HEADERS = {
    Accept: "application/vnd.github+json",
    "X-GitHub-Api-Version": "2022-11-28",
    // Без User-Agent GitHub отвечает 403, а fetch в Workers его не ставит.
    "User-Agent": "transliterator-feedback",
};

/** Токен установки живёт час; берём новый с запасом в пять минут. */
const TOKEN_MARGIN_MS = 5 * 60 * 1000;

let cachedToken = null;

/** Сбрасывает кеш токена — для тестов. */
export function resetTokenCache() {
    cachedToken = null;
}

/**
 * Заводит issue и возвращает его номер и адрес. Ошибку GitHub отдаёт
 * исключением с кодом ответа: страница покажет её как «не отправилось».
 */
export async function createIssue(env, title, body, fetchImpl) {
    const token = await installationToken(env, fetchImpl);

    const response = await fetchImpl(`${API}/repos/${env.GITHUB_REPOSITORY}/issues`, {
        method: "POST",
        headers: { ...HEADERS, Authorization: `Bearer ${token}`, "Content-Type": "application/json" },
        body: JSON.stringify({ title, body }),
    });

    if (!response.ok)
        throw new GitHubError("create issue", response.status);

    const issue = await response.json();
    return { number: issue.number, url: issue.html_url };
}

async function installationToken(env, fetchImpl) {
    if (cachedToken && cachedToken.expires - TOKEN_MARGIN_MS > Date.now())
        return cachedToken.token;

    const jwt = await appJwt(env.GITHUB_APP_ID, env.GITHUB_APP_PRIVATE_KEY);
    const auth = { ...HEADERS, Authorization: `Bearer ${jwt}` };

    // Номер установки не нужно переписывать в настройки руками: GitHub отдаёт
    // его по репозиторию, на который приложение поставлено.
    const installation = await fetchImpl(`${API}/repos/${env.GITHUB_REPOSITORY}/installation`, { headers: auth });
    if (!installation.ok)
        throw new GitHubError("find installation", installation.status);

    const { id } = await installation.json();

    const response = await fetchImpl(`${API}/app/installations/${id}/access_tokens`, { method: "POST", headers: auth });
    if (!response.ok)
        throw new GitHubError("get installation token", response.status);

    const { token, expires_at } = await response.json();
    cachedToken = { token, expires: Date.parse(expires_at) };
    return token;
}

/**
 * JWT приложения: живёт девять минут (GitHub принимает не больше десяти),
 * а выдан минутой раньше — на случай, если часы функции и GitHub расходятся.
 */
export async function appJwt(appId, pem, now = Date.now()) {
    const seconds = Math.floor(now / 1000);
    const header = { alg: "RS256", typ: "JWT" };
    const payload = { iat: seconds - 60, exp: seconds + 9 * 60, iss: String(appId) };

    const unsigned = `${base64Url(JSON.stringify(header))}.${base64Url(JSON.stringify(payload))}`;

    const key = await crypto.subtle.importKey(
        "pkcs8",
        privateKeyDer(pem),
        { name: "RSASSA-PKCS1-v1_5", hash: "SHA-256" },
        false,
        ["sign"],
    );

    const signature = await crypto.subtle.sign("RSASSA-PKCS1-v1_5", key, new TextEncoder().encode(unsigned));
    return `${unsigned}.${base64Url(new Uint8Array(signature))}`;
}

/**
 * Ключ в DER PKCS#8 — другого Web Crypto не читает. GitHub выдаёт ключ
 * приложения в PKCS#1 (`BEGIN RSA PRIVATE KEY`), и он заворачивается здесь,
 * чтобы владельцу не пришлось перегонять его через openssl. Переводы строк,
 * вставленные текстом `\n`, тоже понимаются: так ключ часто попадает в секрет.
 */
export function privateKeyDer(pem) {
    const text = pem.replace(/\\n/g, "\n");
    const pkcs1 = text.includes("BEGIN RSA PRIVATE KEY");
    const base64 = text.replace(/-----(BEGIN|END)[^-]+-----/g, "").replace(/\s+/g, "");
    const der = Uint8Array.from(atob(base64), c => c.charCodeAt(0));

    return pkcs1 ? wrapPkcs1(der) : der;
}

/** PKCS#8 = SEQUENCE { INTEGER 0, SEQUENCE { rsaEncryption, NULL }, OCTET STRING { PKCS#1 } }. */
function wrapPkcs1(pkcs1) {
    const version = [0x02, 0x01, 0x00];
    const algorithm = [0x30, 0x0d, 0x06, 0x09, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01, 0x05, 0x00];
    const octets = [0x04, ...derLength(pkcs1.length), ...pkcs1];
    const content = [...version, ...algorithm, ...octets];

    return new Uint8Array([0x30, ...derLength(content.length), ...content]);
}

function derLength(length) {
    if (length < 0x80)
        return [length];

    const bytes = [];
    for (let rest = length; rest > 0; rest >>= 8)
        bytes.unshift(rest & 0xff);

    return [0x80 | bytes.length, ...bytes];
}

function base64Url(data) {
    const bytes = typeof data === "string" ? new TextEncoder().encode(data) : data;
    let binary = "";
    for (const byte of bytes)
        binary += String.fromCharCode(byte);

    return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

export class GitHubError extends Error {
    constructor(step, status) {
        super(`GitHub: ${step} failed with ${status}`);
        this.status = status;
    }
}
