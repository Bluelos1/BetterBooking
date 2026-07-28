const messages: Record<string, { title: string; body: string; kind: "success" | "error" }> = {
  "listing-created": {
    title: "Listing draft created",
    body: "Your listing is saved as a draft and can be published when ready.",
    kind: "success"
  },
  "listing-published": {
    title: "Listing published",
    body: "The listing is visible in public search and availability checks.",
    kind: "success"
  },
  "listing-unpublished": {
    title: "Listing unpublished",
    body: "The listing moved back to draft and is hidden from public search.",
    kind: "success"
  },
  "listing-archived": {
    title: "Listing archived",
    body: "The listing is no longer public and cannot be republished.",
    kind: "success"
  },
  "reservation-created": {
    title: "Reservation held",
    body: "Your reservation is held as pending. Complete the demo payment from Trips to confirm it.",
    kind: "success"
  },
  "payment-confirmed": {
    title: "Demo payment confirmed",
    body: "The reservation is now confirmed. This is a local demo payment flow, not a real card charge.",
    kind: "success"
  },
  "reservation-cancelled": {
    title: "Reservation cancelled",
    body: "The reservation is cancelled and no longer blocks availability.",
    kind: "success"
  },
  "listing-title-required": {
    title: "Listing title required",
    body: "Enter a listing title before creating a draft.",
    kind: "error"
  },
  "listing-details-required": {
    title: "Listing details required",
    body: "Add the location, description, guest capacity, rooms, and nightly price before creating a draft.",
    kind: "error"
  },
  "listing-create-failed": {
    title: "Listing could not be created",
    body: "The backend rejected the draft listing request.",
    kind: "error"
  },
  "listing-publish-failed": {
    title: "Listing could not be published",
    body: "Check that the listing still exists, belongs to you, and is in a publishable state.",
    kind: "error"
  },
  "listing-unpublish-failed": {
    title: "Listing could not be unpublished",
    body: "Only published listings owned by you can be unpublished.",
    kind: "error"
  },
  "listing-archive-failed": {
    title: "Listing could not be archived",
    body: "Check that the listing exists and belongs to you.",
    kind: "error"
  },
  "reservation-dates-required": {
    title: "Reservation dates required",
    body: "Choose both start and end dates before requesting a reservation.",
    kind: "error"
  },
  "reservation-create-failed": {
    title: "Reservation could not be created",
    body: "The listing may be unavailable or no longer published.",
    kind: "error"
  },
  "reservation-cancel-failed": {
    title: "Reservation could not be cancelled",
    body: "Only the reservation guest can cancel, and the reservation must still be cancellable.",
    kind: "error"
  },
  "payment-confirm-failed": {
    title: "Payment could not be confirmed",
    body: "Only pending unpaid reservations owned by you can be confirmed through the demo payment flow.",
    kind: "error"
  }
};

type ActionNoticeProps = {
  status?: string;
  error?: string;
};

export function ActionNotice({ status, error }: ActionNoticeProps) {
  const message = status ? messages[status] : error ? messages[error] : undefined;

  if (!message) {
    return null;
  }

  return (
    <section className={`action-notice ${message.kind}`} role={message.kind === "error" ? "alert" : "status"}>
      <h2>{message.title}</h2>
      <p>{message.body}</p>
    </section>
  );
}
