import { pluralize } from "@/lib/format";

type PaginationSummaryProps = {
  page: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
};

export function PaginationSummary({ page, pageSize, totalCount, hasNextPage }: PaginationSummaryProps) {
  const firstItem = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const lastItem = Math.min(page * pageSize, totalCount);

  return (
    <p className="pagination-summary">
      Showing {firstItem}-{lastItem} of {pluralize(totalCount, "result")}
      {hasNextPage ? ". More results are available." : "."}
    </p>
  );
}
