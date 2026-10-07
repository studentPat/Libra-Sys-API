-- Run as a DBA against an existing development database.
-- This preserves completed reservation history while allowing a member to
-- reserve the same book again after a reservation is completed or cancelled.
USE librasys;

ALTER TABLE reservations
    DROP INDEX uq_active_reservation;

ALTER TABLE reservations
    ADD COLUMN active_reservation_key VARCHAR(80)
        GENERATED ALWAYS AS (
            IF(status IN ('queued', 'ready'),
               CONCAT(member_id, ':', book_id),
               NULL)
        ) STORED,
    ADD UNIQUE KEY uq_active_reservation (active_reservation_key);

