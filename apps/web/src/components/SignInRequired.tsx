import Link from "next/link";

type SignInRequiredProps = {
  returnTo: string;
  title?: string;
  body?: string;
};

export function SignInRequired({ returnTo, title = "Sign in to continue", body = "Use one account to book stays and manage your own listings." }: SignInRequiredProps) {
  return (
    <section className="notice auth" role="status">
      <h2>{title}</h2>
      <p>{body}</p>
      <Link className="button primary" href={`/sign-in?returnTo=${encodeURIComponent(returnTo)}`}>
        Sign in
      </Link>
    </section>
  );
}
