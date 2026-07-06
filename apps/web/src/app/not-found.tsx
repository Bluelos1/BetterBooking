import Link from "next/link";

export default function NotFound() {
  return (
    <main className="page narrow-page">
      <section className="notice error">
        <p className="eyebrow">404</p>
        <h1>That page is not available.</h1>
        <p>The listing may be unpublished, archived, or missing.</p>
        <Link className="button primary" href="/">Back to listings</Link>
      </section>
    </main>
  );
}
