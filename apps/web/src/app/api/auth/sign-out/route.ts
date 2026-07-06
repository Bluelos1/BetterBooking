import { NextResponse } from "next/server";
import { authSessionCookieName, authTransactionCookieName } from "@/lib/auth/cookies";

export const runtime = "nodejs";

export async function GET(request: Request) {
  return signOut(request);
}

export async function POST(request: Request) {
  return signOut(request);
}

function signOut(request: Request) {
  const response = NextResponse.redirect(new URL("/", process.env.BETTERBOOKING_WEB_BASE_URL?.trim() || request.url));
  response.cookies.delete(authSessionCookieName);
  response.cookies.delete(authTransactionCookieName);

  return response;
}
