import Link from "next/link";
import { getSession } from "@/lib/auth/session";

export async function AppHeader() {
  const session = await getSession();
  const displayName = session?.user.name ?? session?.user.email ?? "Signed in";
  const isAdmin = session?.user.roles.includes("admin") || session?.user.roles.includes("host");

  return (
    <header className="site-header">
      <Link className="brand" href="/" aria-label="BetterBooking home">
        <span className="brand-mark">BB</span>
        <span>BetterBooking</span>
      </Link>
      <nav className="main-nav" aria-label="Main navigation">
        <Link href="/">Explore</Link>
        <Link href="/me/reservations">Trips</Link>
        <Link href="/me/listings">Host dashboard</Link>
        {session ? (
          <form action="/api/auth/sign-out" method="post" className="auth-form">
            <span className={isAdmin ? "role-chip admin" : "role-chip"}>{isAdmin ? "Admin" : "Traveler"}</span>
            <span className="user-chip">{displayName}</span>
            <button className="nav-button" type="submit">Sign out</button>
          </form>
        ) : (
          <>
            <Link href="/sign-in?returnTo=/">Sign in</Link>
            <Link className="nav-cta" href="/create-account">Create account</Link>
          </>
        )}
      </nav>
    </header>
  );
}
