import { createServer } from "node:http";
import { createHash, createSign, generateKeyPairSync, randomBytes } from "node:crypto";

const port = Number(process.env.PORT ?? "8080");
const issuer = trimTrailingSlash(process.env.LOCAL_OIDC_ISSUER ?? "http://localhost:5080");
const internalBaseUrl = trimTrailingSlash(process.env.LOCAL_OIDC_INTERNAL_BASE_URL ?? `http://local-oidc:${port}`);
const audience = process.env.LOCAL_OIDC_AUDIENCE ?? "betterbooking-api";
const clientId = process.env.LOCAL_OIDC_CLIENT_ID ?? "betterbooking-local-web";
const keyId = "betterbooking-local-dev";
const authorizationCodes = new Map();
const personas = {
  guest: {
    sub: process.env.LOCAL_OIDC_GUEST_SUB ?? "local-guest-1",
    name: process.env.LOCAL_OIDC_GUEST_NAME ?? "Maja Traveler",
    email: process.env.LOCAL_OIDC_GUEST_EMAIL ?? "traveler@example.test",
    role: "user",
    label: "Traveler",
    description: "Book stays, check availability, pay demo reservations, and manage trips."
  },
  admin: {
    sub: process.env.LOCAL_OIDC_ADMIN_SUB ?? "local-admin-1",
    name: process.env.LOCAL_OIDC_ADMIN_NAME ?? "Mateusz Host Admin",
    email: process.env.LOCAL_OIDC_ADMIN_EMAIL ?? "host@example.test",
    role: "admin",
    label: "Property admin",
    description: "Create apartments or hotels, publish listings, and manage host inventory."
  }
};
const { privateKey, publicKey } = generateKeyPairSync("rsa", { modulusLength: 2048 });
const publicJwk = {
  ...publicKey.export({ format: "jwk" }),
  kid: keyId,
  use: "sig",
  alg: "RS256"
};

createServer(async (request, response) => {
  try {
    const url = new URL(request.url ?? "/", internalBaseUrl);

    if (request.method === "GET" && url.pathname === "/healthz") {
      return sendJson(response, 200, { status: "ok" });
    }

    if (request.method === "GET" && url.pathname === "/.well-known/openid-configuration") {
      return sendJson(response, 200, {
        issuer,
        authorization_endpoint: `${issuer}/authorize`,
        token_endpoint: `${internalBaseUrl}/token`,
        jwks_uri: `${internalBaseUrl}/jwks`,
        response_types_supported: ["code"],
        subject_types_supported: ["public"],
        id_token_signing_alg_values_supported: ["RS256"],
        scopes_supported: ["openid", "profile", "email", audience],
        token_endpoint_auth_methods_supported: ["none", "client_secret_post"],
        code_challenge_methods_supported: ["S256"]
      });
    }

    if (request.method === "GET" && url.pathname === "/jwks") {
      return sendJson(response, 200, { keys: [publicJwk] });
    }

    if (request.method === "GET" && url.pathname === "/authorize") {
      return authorize(url, response);
    }

    if (request.method === "POST" && url.pathname === "/register") {
      return register(request, response);
    }

    if (request.method === "POST" && url.pathname === "/token") {
      return token(request, response);
    }

    return sendJson(response, 404, { error: "not_found" });
  } catch {
    return sendJson(response, 500, { error: "server_error" });
  }
}).listen(port, "0.0.0.0", () => {
  console.log(`Local OIDC provider listening on ${internalBaseUrl}`);
});

function authorize(url, response) {
  const requestedClientId = url.searchParams.get("client_id");
  const redirectUri = url.searchParams.get("redirect_uri");
  const state = url.searchParams.get("state");
  const codeChallenge = url.searchParams.get("code_challenge");
  const codeChallengeMethod = url.searchParams.get("code_challenge_method");
  const loginHint = url.searchParams.get("login_hint");
  const screenHint = url.searchParams.get("screen_hint");
  const persona = personas[loginHint] ? loginHint : url.searchParams.get("persona");

  if (requestedClientId !== clientId || !redirectUri || !state || !codeChallenge || codeChallengeMethod !== "S256") {
    return sendJson(response, 400, { error: "invalid_request" });
  }

  if (screenHint === "signup") {
    return sendRegistrationPanel(url, response, personas[persona] ? persona : "guest");
  }

  if (!personas[persona]) {
    return sendLoginPanel(url, response);
  }

  return issueAuthorizationCode(response, state, {
    clientId: requestedClientId,
    redirectUri,
    codeChallenge,
    persona
  });
}

async function register(request, response) {
  const form = await readForm(request);
  const requestedClientId = form.get("client_id");
  const redirectUri = form.get("redirect_uri");
  const state = form.get("state");
  const codeChallenge = form.get("code_challenge");
  const codeChallengeMethod = form.get("code_challenge_method");
  const personaKey = personas[form.get("persona")] ? form.get("persona") : "guest";
  const persona = personas[personaKey];
  const name = normalizeFormText(form.get("name"), persona.name, 120);
  const email = normalizeEmail(form.get("email"), persona.email);
  const subject = `local-signup-${personaKey}-${hashForSubject(email)}`;

  if (requestedClientId !== clientId || !redirectUri || !state || !codeChallenge || codeChallengeMethod !== "S256") {
    return sendJson(response, 400, { error: "invalid_request" });
  }

  return issueAuthorizationCode(response, state, {
    clientId: requestedClientId,
    redirectUri,
    codeChallenge,
    persona: personaKey,
    user: {
      sub: subject,
      oid: subject,
      name,
      email,
      role: persona.role
    }
  });
}

function issueAuthorizationCode(response, state, authorizationCode) {
  const code = randomBase64Url(32);
  authorizationCodes.set(code, {
    ...authorizationCode,
    expiresAt: Date.now() + 5 * 60 * 1000
  });

  const callback = new URL(authorizationCode.redirectUri);
  callback.searchParams.set("code", code);
  callback.searchParams.set("state", state);

  response.writeHead(302, { Location: callback.toString() });
  response.end();
}

async function token(request, response) {
  const form = await readForm(request);
  const code = form.get("code");
  const codeVerifier = form.get("code_verifier");
  const requestedClientId = form.get("client_id");
  const redirectUri = form.get("redirect_uri");
  const grantType = form.get("grant_type");
  const authorizationCode = authorizationCodes.get(code ?? "");

  if (
    grantType !== "authorization_code" ||
    !authorizationCode ||
    authorizationCode.expiresAt <= Date.now() ||
    authorizationCode.clientId !== requestedClientId ||
    authorizationCode.redirectUri !== redirectUri ||
    !codeVerifier ||
    authorizationCode.codeChallenge !== createCodeChallenge(codeVerifier)
  ) {
    return sendJson(response, 400, { error: "invalid_grant" });
  }

  authorizationCodes.delete(code);

  const now = Math.floor(Date.now() / 1000);
  const expiresIn = 60 * 60;
  const persona = personas[authorizationCode.persona] ?? personas.guest;
  const userClaims = authorizationCode.user ?? {
    sub: persona.sub,
    oid: persona.sub,
    name: persona.name,
    email: persona.email,
    role: persona.role
  };

  return sendJson(response, 200, {
    access_token: signJwt({
      iss: issuer,
      aud: audience,
      iat: now,
      nbf: now,
      exp: now + expiresIn,
      scope: `openid profile email ${audience}`,
      azp: clientId,
      ...userClaims
    }),
    id_token: signJwt({
      iss: issuer,
      aud: clientId,
      iat: now,
      nbf: now,
      exp: now + expiresIn,
      ...userClaims
    }),
    token_type: "Bearer",
    expires_in: expiresIn,
    scope: `openid profile email ${audience}`
  });
}

function sendLoginPanel(url, response) {
  const guestUrl = withPersona(url, "guest");
  const adminUrl = withPersona(url, "admin");

  response.writeHead(200, {
    "Content-Type": "text/html; charset=utf-8",
    "Cache-Control": "no-store"
  });
  response.end(`<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>BetterBooking Local Sign In</title>
  <style>
    body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: #102016; color: #fffaf0; font-family: Inter, system-ui, sans-serif; }
    main { width: min(920px, calc(100% - 32px)); display: grid; gap: 24px; }
    .panel { border: 1px solid rgb(255 250 240 / 18%); border-radius: 32px; background: rgb(255 250 240 / 8%); padding: clamp(24px, 6vw, 56px); box-shadow: 0 28px 90px rgb(0 0 0 / 32%); }
    h1 { margin: 0; font-family: Georgia, serif; font-size: clamp(42px, 8vw, 88px); letter-spacing: -0.06em; line-height: .95; }
    p { color: rgb(255 250 240 / 72%); line-height: 1.65; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
    a { display: grid; gap: 10px; min-height: 190px; border: 1px solid rgb(255 250 240 / 18%); border-radius: 24px; padding: 24px; color: inherit; text-decoration: none; background: rgb(255 250 240 / 10%); }
    a:hover, a:focus-visible { background: #fffaf0; color: #102016; outline: none; transform: translateY(-2px); }
    strong { font-size: 24px; }
    span { color: inherit; opacity: .76; line-height: 1.55; }
    small { color: rgb(255 250 240 / 55%); }
    @media (max-width: 720px) { .grid { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <main>
    <section class="panel">
      <small>Local development identity provider</small>
      <h1>Choose how to test BetterBooking.</h1>
      <p>This screen exists only in Docker local development. It issues signed local JWTs so the app can test guest and property-admin flows without external secrets.</p>
      <div class="grid">
        <a href="${escapeHtml(guestUrl)}"><strong>${escapeHtml(personas.guest.label)}</strong><span>${escapeHtml(personas.guest.description)}</span></a>
        <a href="${escapeHtml(adminUrl)}"><strong>${escapeHtml(personas.admin.label)}</strong><span>${escapeHtml(personas.admin.description)}</span></a>
      </div>
    </section>
  </main>
</body>
</html>`);
}

function sendRegistrationPanel(url, response, selectedPersona) {
  const hiddenFields = [
    "client_id",
    "redirect_uri",
    "response_type",
    "scope",
    "state",
    "code_challenge",
    "code_challenge_method"
  ].map((name) => `<input type="hidden" name="${name}" value="${escapeHtml(url.searchParams.get(name) ?? "")}" />`).join("\n");

  response.writeHead(200, {
    "Content-Type": "text/html; charset=utf-8",
    "Cache-Control": "no-store"
  });
  response.end(`<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Create BetterBooking Local Account</title>
  <style>
    body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: #102016; color: #fffaf0; font-family: Inter, system-ui, sans-serif; }
    main { width: min(760px, calc(100% - 32px)); }
    form { display: grid; gap: 18px; border: 1px solid rgb(255 250 240 / 18%); border-radius: 32px; background: rgb(255 250 240 / 8%); padding: clamp(24px, 6vw, 56px); box-shadow: 0 28px 90px rgb(0 0 0 / 32%); }
    h1 { margin: 0; font-family: Georgia, serif; font-size: clamp(40px, 8vw, 76px); letter-spacing: -0.06em; line-height: .95; }
    p, span { color: rgb(255 250 240 / 72%); line-height: 1.65; }
    label { display: grid; gap: 8px; font-weight: 800; }
    input[type="text"], input[type="email"] { width: 100%; border: 1px solid rgb(255 250 240 / 18%); border-radius: 16px; background: #fffaf0; color: #102016; padding: 14px 16px; font: inherit; }
    .roles { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
    .role-option { border: 1px solid rgb(255 250 240 / 18%); border-radius: 18px; padding: 16px; background: rgb(255 250 240 / 9%); }
    button { border: 0; border-radius: 999px; background: #fffaf0; color: #102016; cursor: pointer; font: inherit; font-weight: 900; padding: 16px 20px; }
    small { color: rgb(255 250 240 / 55%); }
    @media (max-width: 680px) { .roles { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <main>
    <form method="post" action="/register">
      ${hiddenFields}
      <small>Local development identity provider</small>
      <h1>Create your local account.</h1>
      <p>This simulates provider-side registration for local testing. No password is stored by BetterBooking.</p>
      <label>
        Full name
        <input name="name" type="text" autocomplete="name" maxlength="120" placeholder="Alex Morgan" required />
      </label>
      <label>
        Email
        <input name="email" type="email" autocomplete="email" maxlength="320" placeholder="alex@example.test" required />
      </label>
      <div class="roles" role="radiogroup" aria-label="Account type">
        <label class="role-option">
          <input type="radio" name="persona" value="guest" ${selectedPersona === "guest" ? "checked" : ""} />
          Traveler
          <span>Book stays and manage trips.</span>
        </label>
        <label class="role-option">
          <input type="radio" name="persona" value="admin" ${selectedPersona === "admin" ? "checked" : ""} />
          Property owner
          <span>Create and publish hotels or apartments.</span>
        </label>
      </div>
      <button type="submit">Create account and continue</button>
    </form>
  </main>
</body>
</html>`);
}

function withPersona(url, persona) {
  const next = new URL(url.toString());
  next.searchParams.set("persona", persona);

  return next.toString();
}

function signJwt(payload) {
  const header = base64UrlJson({ alg: "RS256", typ: "JWT", kid: keyId });
  const body = base64UrlJson(payload);
  const signingInput = `${header}.${body}`;
  const signature = createSign("RSA-SHA256").update(signingInput).sign(privateKey, "base64url");

  return `${signingInput}.${signature}`;
}

function createCodeChallenge(codeVerifier) {
  return createHash("sha256").update(codeVerifier).digest("base64url");
}

function base64UrlJson(value) {
  return Buffer.from(JSON.stringify(value)).toString("base64url");
}

function randomBase64Url(size) {
  return randomBytes(size).toString("base64url");
}

async function readForm(request) {
  const chunks = [];

  for await (const chunk of request) {
    chunks.push(chunk);
  }

  return new URLSearchParams(Buffer.concat(chunks).toString("utf8"));
}

function sendJson(response, statusCode, body) {
  response.writeHead(statusCode, {
    "Content-Type": "application/json",
    "Cache-Control": "no-store"
  });
  response.end(JSON.stringify(body));
}

function trimTrailingSlash(value) {
  return value.replace(/\/+$/, "");
}

function normalizeFormText(value, fallback, maxLength) {
  const normalized = String(value ?? "").trim().replace(/\s+/g, " ").slice(0, maxLength);

  return normalized || fallback;
}

function normalizeEmail(value, fallback) {
  const normalized = String(value ?? "").trim().toLowerCase().slice(0, 320);

  return normalized.includes("@") ? normalized : fallback;
}

function hashForSubject(value) {
  return createHash("sha256").update(value).digest("hex").slice(0, 24);
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}
