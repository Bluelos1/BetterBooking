import Link from "next/link";

type SignInRequiredProps = {
  returnTo: string;
};

export function SignInRequired({ returnTo }: SignInRequiredProps) {
  return (
    <section className="notice auth" role="status">
      <p className="eyebrow">Authentication required</p>
      <h2>Sign in to view this workspace.</h2>
      <p>
        This page calls protected backend APIs with the server-side access token after sign-in.
      </p>
      <Link className="button primary" href={`/sign-in?returnTo=${encodeURIComponent(returnTo)}`}>
        Sign in
      </Link>
    </section>
  );
}
