import Link from "next/link";
import { sanitizeReturnTo } from "@/lib/auth/oidc";

type CreateAccountPageProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function CreateAccountPage({ searchParams }: CreateAccountPageProps) {
  const params = await searchParams;
  const returnTo = sanitizeReturnTo(getSingle(params.returnTo));

  return (
    <main className="page sign-in-page">
      <section className="login-hero account-hero">
        <p className="eyebrow">Create account</p>
        <h1>Create your BetterBooking account.</h1>
        <p>Book stays or publish your apartment or hotel with the same account.</p>
        <div className="auth-actions">
          <Link className="button primary" href={`/api/auth/sign-in?screen=signup&returnTo=${encodeURIComponent(returnTo)}`}>Create account</Link>
          <Link className="text-link" href={`/sign-in?returnTo=${encodeURIComponent(returnTo)}`}>Already have an account? Sign in</Link>
        </div>
      </section>
    </main>
  );
}

function getSingle(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}
