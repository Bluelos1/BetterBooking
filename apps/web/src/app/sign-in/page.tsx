import Link from "next/link";
import { sanitizeReturnTo } from "@/lib/auth/oidc";

type SignInPageProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function SignInPage({ searchParams }: SignInPageProps) {
  const params = await searchParams;
  const returnTo = sanitizeReturnTo(getSingle(params.returnTo));

  return (
    <main className="page sign-in-page">
      <section className="login-hero">
        <p className="eyebrow">Sign in</p>
        <h1>Sign in to BetterBooking.</h1>
        <p>One account lets you book stays, manage trips, and publish your own properties.</p>
        <div className="auth-actions">
          <Link className="button primary" href={`/api/auth/sign-in?returnTo=${encodeURIComponent(returnTo)}`}>Continue to sign in</Link>
          <Link className="text-link" href={`/create-account?returnTo=${encodeURIComponent(returnTo)}`}>Create an account</Link>
        </div>
      </section>
    </main>
  );
}

function getSingle(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}
