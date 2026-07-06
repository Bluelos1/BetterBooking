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
        <h1>Start as a traveler or property owner.</h1>
        <p>
          Production BetterBooking should delegate password storage, verification, MFA, and recovery to a real
          identity provider. Locally, Docker simulates that provider so you can test account creation without secrets.
        </p>
      </section>

      <section className="persona-grid" aria-label="Account types">
        <AccountCard
          title="Traveler account"
          eyebrow="Book stays"
          body="Create a guest identity for searching, reserving, demo payment confirmation, and trip management."
          href={`/api/auth/sign-in?screen=signup&persona=guest&returnTo=${encodeURIComponent(returnTo === "/" ? "/me/reservations" : returnTo)}`}
          cta="Create traveler account"
        />
        <AccountCard
          title="Property owner account"
          eyebrow="Host apartments or hotels"
          body="Create a host identity for listing setup, publishing, and property lifecycle management."
          href={`/api/auth/sign-in?screen=signup&persona=admin&returnTo=${encodeURIComponent(returnTo === "/" ? "/me/listings" : returnTo)}`}
          cta="Create owner account"
          featured
        />
      </section>

      <section className="account-flow-note">
        <p className="eyebrow">Already registered?</p>
        <p><Link className="text-link" href={`/sign-in?returnTo=${encodeURIComponent(returnTo)}`}>Sign in to an existing local account</Link></p>
      </section>
    </main>
  );
}

function AccountCard({
  title,
  eyebrow,
  body,
  href,
  cta,
  featured = false
}: {
  title: string;
  eyebrow: string;
  body: string;
  href: string;
  cta: string;
  featured?: boolean;
}) {
  return (
    <article className={featured ? "persona-card featured" : "persona-card"}>
      <p className="eyebrow">{eyebrow}</p>
      <h2>{title}</h2>
      <p>{body}</p>
      <Link className="button primary" href={href}>{cta}</Link>
    </article>
  );
}

function getSingle(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}
