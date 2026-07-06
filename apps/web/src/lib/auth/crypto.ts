import { createCipheriv, createDecipheriv, createHash, randomBytes } from "node:crypto";

const algorithm = "aes-256-gcm";

export function encryptJson(value: unknown, secret: string): string {
  const iv = randomBytes(12);
  const key = deriveKey(secret);
  const cipher = createCipheriv(algorithm, key, iv);
  const encrypted = Buffer.concat([
    cipher.update(JSON.stringify(value), "utf8"),
    cipher.final()
  ]);
  const tag = cipher.getAuthTag();

  return [iv, encrypted, tag].map((part) => part.toString("base64url")).join(".");
}

export function decryptJson<T>(value: string, secret: string): T | null {
  const [ivValue, encryptedValue, tagValue] = value.split(".");

  if (!ivValue || !encryptedValue || !tagValue) {
    return null;
  }

  try {
    const decipher = createDecipheriv(algorithm, deriveKey(secret), Buffer.from(ivValue, "base64url"));
    decipher.setAuthTag(Buffer.from(tagValue, "base64url"));
    const decrypted = Buffer.concat([
      decipher.update(Buffer.from(encryptedValue, "base64url")),
      decipher.final()
    ]);

    return JSON.parse(decrypted.toString("utf8")) as T;
  } catch {
    return null;
  }
}

function deriveKey(secret: string): Buffer {
  return createHash("sha256").update(secret).digest();
}
