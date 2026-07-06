-- Reviewed PostgreSQL shape for the first booking schema.
-- This file is intentionally secret-free and should be translated into an EF Core migration before applying to shared environments.

CREATE EXTENSION IF NOT EXISTS btree_gist;

CREATE TABLE IF NOT EXISTS users (
    id uuid PRIMARY KEY,
    external_provider varchar(200) NOT NULL,
    external_subject varchar(300) NOT NULL,
    email varchar(320),
    display_name varchar(200),
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_external_provider_external_subject
    ON users (external_provider, external_subject);

CREATE TABLE IF NOT EXISTS audit_events (
    id uuid PRIMARY KEY,
    event_type varchar(100) NOT NULL,
    actor_user_id uuid,
    subject_type varchar(100) NOT NULL,
    subject_id uuid,
    created_at timestamp with time zone NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_audit_events_created_at
    ON audit_events (created_at);

CREATE INDEX IF NOT EXISTS ix_audit_events_actor_user_id_created_at
    ON audit_events (actor_user_id, created_at);

CREATE TABLE IF NOT EXISTS listings (
    id uuid PRIMARY KEY,
    owner_user_id uuid NOT NULL,
    title varchar(200) NOT NULL,
    description varchar(2000) NOT NULL,
    location varchar(160) NOT NULL,
    nightly_price_amount numeric(12, 2) NOT NULL,
    max_guests integer NOT NULL,
    bedroom_count integer NOT NULL,
    bathroom_count integer NOT NULL,
    hero_image_url varchar(2048) NOT NULL,
    amenities varchar(500) NOT NULL,
    status varchar(32) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT fk_listings_users_owner_user_id FOREIGN KEY (owner_user_id) REFERENCES users (id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_listings_status
    ON listings (status);

CREATE INDEX IF NOT EXISTS ix_listings_owner_user_id_status
    ON listings (owner_user_id, status);

CREATE INDEX IF NOT EXISTS ix_listings_location
    ON listings (location);

CREATE TABLE IF NOT EXISTS reservations (
    id uuid PRIMARY KEY,
    listing_id uuid NOT NULL,
    guest_user_id uuid NOT NULL,
    start_date date NOT NULL,
    end_date date NOT NULL,
    status varchar(32) NOT NULL,
    payment_status varchar(32) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT fk_reservations_listings_listing_id FOREIGN KEY (listing_id) REFERENCES listings (id) ON DELETE RESTRICT,
    CONSTRAINT fk_reservations_users_guest_user_id FOREIGN KEY (guest_user_id) REFERENCES users (id) ON DELETE RESTRICT,
    CONSTRAINT ck_reservations_period_valid CHECK (end_date > start_date)
);

CREATE INDEX IF NOT EXISTS ix_reservations_listing_id_status
    ON reservations (listing_id, status);

CREATE INDEX IF NOT EXISTS ix_reservations_guest_user_id_created_at
    ON reservations (guest_user_id, created_at);

ALTER TABLE reservations
    DROP CONSTRAINT IF EXISTS ex_reservations_no_active_overlap;

ALTER TABLE reservations
    ADD CONSTRAINT ex_reservations_no_active_overlap
    EXCLUDE USING gist (
        listing_id WITH =,
        daterange(start_date, end_date, '[)') WITH &&
    )
    WHERE (status IN ('Pending', 'Confirmed'));
