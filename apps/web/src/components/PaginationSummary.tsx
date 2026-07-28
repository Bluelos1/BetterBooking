import Link from "next/link";
import { pluralize } from "@/lib/format";

type PaginationSummaryProps = {
  page: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
  basePath: string;
  query?: Record<string, string | undefined>;
};

export function PaginationSummary({ page, pageSize, totalCount, hasNextPage, basePath, query = {} }: PaginationSummaryProps) {
  const pageStart = (page - 1) * pageSize;
  const firstItem = totalCount === 0 || pageStart >= totalCount ? 0 : pageStart + 1;
  const lastItem = firstItem === 0 ? 0 : Math.min(page * pageSize, totalCount);

  return (
    <nav className="pagination" aria-label="Pagination">
      <span>{firstItem === 0 ? pluralize(totalCount, "result") : `Showing ${firstItem}-${lastItem} of ${pluralize(totalCount, "result")}`}</span>
      <div className="pagination-links">
        {page > 1 ? <Link href={buildPageHref(basePath, page - 1, query)}>Previous</Link> : null}
        {hasNextPage ? <Link href={buildPageHref(basePath, page + 1, query)}>Next</Link> : null}
      </div>
    </nav>
  );
}

function buildPageHref(basePath: string, page: number, query: Record<string, string | undefined>): string {
  const params = new URLSearchParams();

  for (const [key, value] of Object.entries(query)) {
    if (value) params.set(key, value);
  }

  if (page > 1) params.set("page", String(page));

  const search = params.toString();
  return search ? `${basePath}?${search}` : basePath;
}
