import Link from "next/link";
import { getSession } from "@/lib/auth/session";

export async function AppHeader() {
  const session = await getSession();
  const displayName = session?.user.name ?? session?.user.email ?? "Signed in";

  return (
    <header className="site-header">
      <div className="header-inner">
        <Link className="brand" href="/" aria-label="BetterBooking home">
          <span className="brand-mark">BB</span>
          <span className="brand-name">BetterBooking</span>
        </Link>
        <nav className="main-nav" aria-label="Main navigation">
          <Link href="/">Stays</Link>
          {session ? <Link href="/me/reservations">Trips</Link> : null}
          {session ? <Link href="/me/listings">Hosting</Link> : null}
        </nav>
        {session ? (
          <form action="/api/auth/sign-out" method="post" className="auth-form">
            <span className="user-chip">{displayName}</span>
            <button className="nav-button" type="submit">Sign out</button>
          </form>
        ) : (
          <div className="auth-links">
            <Link href="/sign-in?returnTo=/">Sign in</Link>
            <Link className="nav-cta" href="/create-account">Create account</Link>
          </div>
        )}
      </div>
    </header>
  );
}
