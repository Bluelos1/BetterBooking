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
        <h1>Welcome back to your workspace.</h1>
        <p>
          Sign in as a traveler to manage trips, or as a property admin to manage apartments and hotels.
          New users can create an account first.
        </p>
        <Link className="text-link" href={`/create-account?returnTo=${encodeURIComponent(returnTo)}`}>Create a new account</Link>
      </section>

      <section className="persona-grid" aria-label="Sign-in choices">
        <PersonaCard
          title="Traveler"
          eyebrow="Guest account"
          body="Search, check dates, reserve a stay, run the demo payment, and manage trip history."
          href={`/api/auth/sign-in?persona=guest&returnTo=${encodeURIComponent(returnTo)}`}
          cta="Continue as traveler"
        />
        <PersonaCard
          title="Property admin"
          eyebrow="Host account"
          body="Create hotel or apartment listings with details, publish them, and manage lifecycle actions."
          href={`/api/auth/sign-in?persona=admin&returnTo=${encodeURIComponent(returnTo === "/" ? "/me/listings" : returnTo)}`}
          cta="Continue as admin"
          featured
        />
      </section>
    </main>
  );
}

function PersonaCard({
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
