-- DottIn demo dataset: complete August 2026 (last complete month on 2026-09-22).
-- DEVELOPMENT/QA ONLY. Apply migrations first. Never run against production.
-- Non-destructive and rerunnable: inserts only missing rows for the demo IDs.
-- Existing seed employees are updated only for their demo role and invalid legacy CPF.
-- Login password and PIN for every demo user: 123456.
-- Run: psql -X -v ON_ERROR_STOP=1 -d dottindb -f backend/tools/seed_demo_august_2026.sql

BEGIN;

DO $$
BEGIN
    IF to_regclass('"TimeKeepingAdjustments"') IS NULL
       OR to_regclass('"TimeEntries"') IS NULL
       OR to_regclass('"TenantSubscriptions"') IS NULL THEN
        RAISE EXCEPTION 'Apply all DottIn migrations before loading the demo month';
    END IF;
END $$;

-- Same five branches and IDs as the supplied mock. Insert branches before owners.
INSERT INTO "Branches" (
    "Id", "Name", "CompanyCode", "Document", "DocumentType", "Email", "PhoneNumber",
    "Street", "Number", "Complement", "City", "State", "ZipCode",
    "Latitude", "Longitude", "TimeZoneId", "AllowedRadiusMeters", "ToleranceMinutes",
    "HolidayCalendarId", "AllowOvernightShifts", "IsActive", "IsHeadquarters", "OwnerId",
    "StartWorkTime", "EndWorkTime", "CreatedAt", "UpdatedAt"
) VALUES
('a1b2c3d4-e5f6-7890-abcd-ef1234567890', 'DottIn Headquarters', 'DOTTIN-HQ-001', '12345678000195', 'CNPJ', 'hq@dottin.com', '11999998888', 'Av. Paulista', 1000, 'Sala 801', 'Sao Paulo', 'SP', '01311100', -23.5613, -46.6560, 'America/Sao_Paulo', 500, 15, NULL, false, true, true, NULL, '08:00', '17:00', now(), NULL),
('b2c3d4e5-f6a7-8901-bcde-f12345678901', 'DottIn Branch Rio', 'DOTTIN-RJ-002', '98765432000195', 'CNPJ', 'rio@dottin.com', '21999997777', 'Av. Rio Branco', 500, 'Andar 10', 'Rio de Janeiro', 'RJ', '20040001', -22.9068, -43.1729, 'America/Sao_Paulo', 500, 15, NULL, false, true, false, NULL, '08:00', '17:00', now(), NULL),
('11111111-2222-3333-4444-555555555555', 'Googleplex HQ', 'GOOGLEPLEX-001', '12345678901234', 'CNPJ', 'googleplex@dottin.com', '16505551234', 'Amphitheatre Pkwy', 1600, NULL, 'Mountain View', 'CA', '94043', 37.4220, -122.0840, 'America/Los_Angeles', 500, 15, NULL, false, true, true, NULL, '08:00', '17:00', now(), NULL),
('11111111-2222-3333-4444-666666666666', 'Google San Francisco', 'GOOGLEPLEX-SF-002', '12345678905678', 'CNPJ', 'sf@googleplex.com', '14155559876', 'Spear St', 345, 'Floor 8', 'San Francisco', 'CA', '94105', 37.7903, -122.3935, 'America/Los_Angeles', 500, 15, NULL, false, true, false, NULL, '08:00', '17:00', now(), NULL),
('11111111-2222-3333-4444-777777777777', 'Google New York', 'GOOGLEPLEX-NY-003', '12345678909012', 'CNPJ', 'nyc@googleplex.com', '12125551234', '8th Ave', 111, 'Floor 15', 'New York', 'NY', '10011', 40.7415, -74.0023, 'America/New_York', 500, 15, NULL, false, true, false, NULL, '09:00', '18:00', now(), NULL)
ON CONFLICT ("Id") DO NOTHING;

-- Valid CPF check digits, explicit roles, distinct schedules, one inactive account.
-- Retains employee IDs from the supplied mock for existing links/bookmarks.
INSERT INTO "Employees" (
    "Id", "Name", "CPF", "DocumentType", "ImageUrl", "BranchId",
    "PasswordHash", "PinHash", "FingerprintHash", "StartWorkTime", "EndWorkTime",
    "IntervalStart", "IntervalEnd", "CreatedAt", "UpdatedAt", "IsActive",
    "AllowOvernightShifts", "Role"
)
SELECT v.id, v.name, v.cpf, 'CPF', NULL, v.branch_id,
       '$2a$11$YoKSXY1868hVYYatvSmK.OdBnhftFPinSseMKhcQP8xnZwWHs9EOW',
       '$2a$11$YoKSXY1868hVYYatvSmK.OdBnhftFPinSseMKhcQP8xnZwWHs9EOW',
       NULL, v.start_at, v.end_at, v.break_at, v.resume_at, now(), NULL,
       v.active, false, v.role
FROM (VALUES
    ('aaaa1111-bbbb-cccc-dddd-eeee11111111'::uuid, 'Roberto Almeida', '10020030088', 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid, '08:00'::time, '17:00'::time, '12:00'::time, '13:00'::time, true, 'Owner'),
    ('c3d4e5f6-a7b8-9012-cdef-123456789012', 'Joao Silva', '11122233396', 'a1b2c3d4-e5f6-7890-abcd-ef1234567890', '08:00', '17:00', '12:00', '13:00', true, 'Employee'),
    ('d4e5f6a7-b8c9-0123-def0-234567890123', 'Maria Oliveira', '55566677720', 'a1b2c3d4-e5f6-7890-abcd-ef1234567890', '08:00', '17:00', '12:00', '13:00', true, 'Manager'),
    ('aaaa2222-bbbb-cccc-dddd-eeee22222222', 'Pedro Lima', '22233344405', 'a1b2c3d4-e5f6-7890-abcd-ef1234567890', '09:00', '18:00', '12:30', '13:30', true, 'Administrator'),
    ('f6a7b8c9-d0e1-2345-f012-456789012345', 'Ana Paula Ex-Func', '12312312387', 'a1b2c3d4-e5f6-7890-abcd-ef1234567890', '08:00', '17:00', '12:00', '13:00', false, 'Employee'),
    ('bbbb1111-cccc-dddd-eeee-ffff11111111', 'Fernanda Costa', '33344455508', 'b2c3d4e5-f6a7-8901-bcde-f12345678901', '08:00', '17:00', '12:00', '13:00', true, 'Owner'),
    ('e5f6a7b8-c9d0-1234-ef01-345678901234', 'Carlos Santos', '99988877714', 'b2c3d4e5-f6a7-8901-bcde-f12345678901', '08:00', '17:00', '12:00', '13:00', true, 'Employee'),
    ('bbbb2222-cccc-dddd-eeee-ffff22222222', 'Lucas Mendes', '44455566619', 'b2c3d4e5-f6a7-8901-bcde-f12345678901', '07:00', '16:00', '11:00', '12:00', true, 'Manager'),
    ('bbbb3333-cccc-dddd-eeee-ffff33333333', 'Juliana Ferreira', '66677788830', 'b2c3d4e5-f6a7-8901-bcde-f12345678901', '08:00', '17:00', '12:00', '13:00', true, 'Employee'),
    ('22222222-3333-4444-5555-666666666666', 'Test User', '12345678909', '11111111-2222-3333-4444-555555555555', '08:00', '17:00', '12:00', '13:00', true, 'Owner'),
    ('cccc1111-dddd-eeee-ffff-111122223333', 'Sarah Connor', '77788899941', '11111111-2222-3333-4444-555555555555', '08:00', '17:00', '12:00', '13:00', true, 'Administrator'),
    ('cccc2222-dddd-eeee-ffff-222233334444', 'James Kirk', '88899900078', '11111111-2222-3333-4444-555555555555', '09:00', '18:00', '13:00', '14:00', true, 'Employee'),
    ('dddd1111-eeee-ffff-0000-111122223333', 'Ellen Ripley', '10111213100', '11111111-2222-3333-4444-666666666666', '08:00', '17:00', '12:00', '13:00', true, 'Manager'),
    ('dddd2222-eeee-ffff-0000-222233334444', 'Tony Stark', '20212223224', '11111111-2222-3333-4444-666666666666', '09:00', '18:00', '12:30', '13:30', true, 'Employee'),
    ('dddd3333-eeee-ffff-0000-333344445555', 'Bruce Wayne', '30313233357', '11111111-2222-3333-4444-666666666666', '08:00', '17:00', '12:00', '13:00', true, 'Employee'),
    ('eeee1111-ffff-0000-1111-222233334444', 'Peter Parker', '40414243480', '11111111-2222-3333-4444-777777777777', '09:00', '18:00', '12:00', '13:00', true, 'Employee'),
    ('eeee2222-ffff-0000-1111-333344445555', 'Diana Prince', '50515253502', '11111111-2222-3333-4444-777777777777', '09:00', '18:00', '13:00', '14:00', true, 'Employee'),
    ('eeee3333-ffff-0000-1111-444455556666', 'Clark Kent', '60616263635', '11111111-2222-3333-4444-777777777777', '08:00', '17:00', '12:00', '13:00', true, 'Employee')
) AS v(id, name, cpf, branch_id, start_at, end_at, break_at, resume_at, active, role)
ON CONFLICT ("Id") DO NOTHING;

-- Upgrade only the exact legacy CPF values from the supplied mock, preserving other edits.
UPDATE "Employees" AS e SET "CPF" = v.valid_cpf
FROM (VALUES
    ('aaaa1111-bbbb-cccc-dddd-eeee11111111'::uuid, '10020030040', '10020030088'),
    ('c3d4e5f6-a7b8-9012-cdef-123456789012', '11122233344', '11122233396'),
    ('d4e5f6a7-b8c9-0123-def0-234567890123', '55566677788', '55566677720'),
    ('aaaa2222-bbbb-cccc-dddd-eeee22222222', '22233344455', '22233344405'),
    ('f6a7b8c9-d0e1-2345-f012-456789012345', '12312312322', '12312312387'),
    ('bbbb1111-cccc-dddd-eeee-ffff11111111', '33344455566', '33344455508'),
    ('e5f6a7b8-c9d0-1234-ef01-345678901234', '99988877766', '99988877714'),
    ('bbbb2222-cccc-dddd-eeee-ffff22222222', '44455566677', '44455566619'),
    ('bbbb3333-cccc-dddd-eeee-ffff33333333', '66677788899', '66677788830'),
    ('22222222-3333-4444-5555-666666666666', '12345678900', '12345678909'),
    ('cccc1111-dddd-eeee-ffff-111122223333', '77788899900', '77788899941'),
    ('cccc2222-dddd-eeee-ffff-222233334444', '88899900011', '88899900078'),
    ('dddd1111-eeee-ffff-0000-111122223333', '10111213140', '10111213100'),
    ('dddd2222-eeee-ffff-0000-222233334444', '20212223240', '20212223224'),
    ('dddd3333-eeee-ffff-0000-333344445555', '30313233340', '30313233357'),
    ('eeee1111-ffff-0000-1111-222233334444', '40414243440', '40414243480'),
    ('eeee2222-ffff-0000-1111-333344445555', '50515253540', '50515253502'),
    ('eeee3333-ffff-0000-1111-444455556666', '60616263640', '60616263635')
) AS v(id, old_cpf, valid_cpf)
WHERE e."Id" = v.id AND e."CPF" = v.old_cpf;

UPDATE "Branches" AS b SET "OwnerId" = v.owner_id
FROM (VALUES
    ('a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid, 'aaaa1111-bbbb-cccc-dddd-eeee11111111'::uuid),
    ('b2c3d4e5-f6a7-8901-bcde-f12345678901', 'bbbb1111-cccc-dddd-eeee-ffff11111111'),
    ('11111111-2222-3333-4444-555555555555', '22222222-3333-4444-5555-666666666666'),
    ('11111111-2222-3333-4444-666666666666', '22222222-3333-4444-5555-666666666666'),
    ('11111111-2222-3333-4444-777777777777', '22222222-3333-4444-5555-666666666666')
) AS v(branch_id, owner_id)
WHERE b."Id" = v.branch_id AND b."OwnerId" IS NULL;

-- The old mock inserted every account with the default Employee role.
UPDATE "Employees" AS e SET "Role" = v.role
FROM (VALUES
    ('aaaa1111-bbbb-cccc-dddd-eeee11111111'::uuid, 'Owner'),
    ('d4e5f6a7-b8c9-0123-def0-234567890123', 'Manager'),
    ('aaaa2222-bbbb-cccc-dddd-eeee22222222', 'Administrator'),
    ('bbbb1111-cccc-dddd-eeee-ffff11111111', 'Owner'),
    ('bbbb2222-cccc-dddd-eeee-ffff22222222', 'Manager'),
    ('22222222-3333-4444-5555-666666666666', 'Owner'),
    ('cccc1111-dddd-eeee-ffff-111122223333', 'Administrator'),
    ('dddd1111-eeee-ffff-0000-111122223333', 'Manager')
) AS v(id, role)
WHERE e."Id" = v.id AND e."Role" = 'Employee';

-- A local-only plan opens the operational screens without a real Stripe charge.
INSERT INTO "SubscriptionPlans" (
    "Id", "Name", "StripePriceId", "MaxEmployees", "MaxBranches",
    "MonthlyPriceBRL", "FeaturesJson", "IsActive", "CreatedAt", "UpdatedAt"
) VALUES (
    'f00d2026-0000-4000-8000-000000000001', 'Demo QA', NULL, 50, 10,
    0, '{"demo":true}'::jsonb, true, now(), NULL
)
ON CONFLICT DO NOTHING;

INSERT INTO "TenantSubscriptions" (
    "Id", "HeadquartersId", "OwnerId", "StripeCustomerId", "StripeSubscriptionId",
    "SubscriptionPlanId", "Status", "CurrentPeriodStart", "CurrentPeriodEnd",
    "CanceledAt", "CreatedAt", "UpdatedAt"
)
SELECT md5('demo-subscription:' || v.owner_id::text)::uuid, v.hq_id, v.owner_id,
       'cus_demo_' || replace(v.owner_id::text, '-', ''), NULL,
       p."Id", 'Free', now() - interval '1 day', now() + interval '1 year',
       NULL, now(), NULL
FROM (VALUES
    ('a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid, 'aaaa1111-bbbb-cccc-dddd-eeee11111111'::uuid),
    ('b2c3d4e5-f6a7-8901-bcde-f12345678901', 'bbbb1111-cccc-dddd-eeee-ffff11111111'),
    ('11111111-2222-3333-4444-555555555555', '22222222-3333-4444-5555-666666666666')
) AS v(hq_id, owner_id)
JOIN "SubscriptionPlans" AS p ON p."Name" = 'Demo QA'
ON CONFLICT DO NOTHING;

-- One active 2026 calendar per branch. Reuse an existing active calendar if present.
INSERT INTO "HolidayCalendars" (
    "Id", "BranchId", "Name", "Description", "CountryCode", "RegionCode",
    "Year", "IsActive", "CreatedAt", "UpdatedAt"
)
SELECT md5('demo-calendar-2026:' || b."Id"::text)::uuid,
       b."Id", 'Calendario demo 2026', 'Feriados de teste; nao oficiais',
       CASE WHEN b."State" IN ('SP', 'RJ') THEN 'BR' ELSE 'US' END,
       b."State", 2026, true, now(), NULL
FROM "Branches" AS b
WHERE b."Id" IN (
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'b2c3d4e5-f6a7-8901-bcde-f12345678901',
    '11111111-2222-3333-4444-555555555555',
    '11111111-2222-3333-4444-666666666666',
    '11111111-2222-3333-4444-777777777777'
)
AND NOT EXISTS (
    SELECT 1 FROM "HolidayCalendars" AS hc
    WHERE hc."BranchId" = b."Id" AND hc."Year" = 2026 AND hc."IsActive"
);

CREATE TEMP TABLE demo_calendar ON COMMIT DROP AS
SELECT DISTINCT ON (hc."BranchId") hc."BranchId", hc."Id" AS calendar_id
FROM "HolidayCalendars" AS hc
WHERE hc."Year" = 2026 AND hc."IsActive"
  AND hc."BranchId" IN (
      'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
      'b2c3d4e5-f6a7-8901-bcde-f12345678901',
      '11111111-2222-3333-4444-555555555555',
      '11111111-2222-3333-4444-666666666666',
      '11111111-2222-3333-4444-777777777777'
  )
ORDER BY hc."BranchId", hc."CreatedAt" DESC;

UPDATE "Branches" AS b SET "HolidayCalendarId" = c.calendar_id
FROM demo_calendar AS c
WHERE b."Id" = c."BranchId" AND b."HolidayCalendarId" IS NULL;

INSERT INTO "Holidays" ("Date", "Name", "Type", "IsOptional", "HolidayCalendarId")
SELECT v.day, v.name, 'Company', false, c.calendar_id
FROM (VALUES
    ('a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid, '2026-08-14'::date, 'Dia da equipe SP (demo)'),
    ('b2c3d4e5-f6a7-8901-bcde-f12345678901', '2026-08-21', 'Dia da equipe RJ (demo)'),
    ('11111111-2222-3333-4444-555555555555', '2026-08-10', 'Team day Mountain View (demo)'),
    ('11111111-2222-3333-4444-666666666666', '2026-08-17', 'Team day San Francisco (demo)'),
    ('11111111-2222-3333-4444-777777777777', '2026-08-24', 'Team day New York (demo)')
) AS v(branch_id, day, name)
JOIN demo_calendar AS c ON c."BranchId" = v.branch_id
ON CONFLICT ("HolidayCalendarId", "Date") DO NOTHING;

-- One row per active employee per business day, with five explicit absences,
-- one Saturday shift per branch and one holiday shift per branch.
CREATE TEMP TABLE demo_shifts ON COMMIT DROP AS
SELECT e."Id" AS employee_id, e."BranchId" AS branch_id,
       md5('demo-shift:' || e."Id"::text || ':' || d.day::text)::uuid AS timekeeping_id,
       d.day AS work_date, b."TimeZoneId" AS time_zone,
       b."Latitude" AS latitude, b."Longitude" AS longitude,
       CASE (extract(day FROM d.day)::int + ascii(substr(e."Id"::text, 1, 1))) % 3
           WHEN 0 THEN 'Mobile' WHEN 1 THEN 'Web' ELSE 'Kiosk' END AS source,
       d.day + e."StartWorkTime" +
           CASE WHEN extract(day FROM d.day)::int % 9 = 0 THEN interval '29 minutes'
                WHEN extract(day FROM d.day)::int % 6 = 0 THEN interval '-8 minutes'
                ELSE interval '3 minutes' END AS clock_in_local,
       d.day + e."IntervalStart" AS break_start_local,
       d.day + e."IntervalEnd" +
           CASE WHEN extract(day FROM d.day)::int % 8 = 0 THEN interval '12 minutes'
                ELSE interval '0 minutes' END AS break_end_local,
       d.day + e."EndWorkTime" +
           CASE WHEN extract(day FROM d.day)::int % 11 = 0 THEN interval '-42 minutes'
                WHEN extract(day FROM d.day)::int % 7 = 0 THEN interval '68 minutes'
                ELSE interval '4 minutes' END AS clock_out_local
FROM "Employees" AS e
JOIN "Branches" AS b ON b."Id" = e."BranchId"
CROSS JOIN LATERAL generate_series('2026-08-01'::date, '2026-08-31'::date, interval '1 day') AS d(day)
WHERE e."Id" IN (
    'aaaa1111-bbbb-cccc-dddd-eeee11111111', 'c3d4e5f6-a7b8-9012-cdef-123456789012',
    'd4e5f6a7-b8c9-0123-def0-234567890123', 'aaaa2222-bbbb-cccc-dddd-eeee22222222',
    'f6a7b8c9-d0e1-2345-f012-456789012345', 'bbbb1111-cccc-dddd-eeee-ffff11111111',
    'e5f6a7b8-c9d0-1234-ef01-345678901234', 'bbbb2222-cccc-dddd-eeee-ffff22222222',
    'bbbb3333-cccc-dddd-eeee-ffff33333333', '22222222-3333-4444-5555-666666666666',
    'cccc1111-dddd-eeee-ffff-111122223333', 'cccc2222-dddd-eeee-ffff-222233334444',
    'dddd1111-eeee-ffff-0000-111122223333', 'dddd2222-eeee-ffff-0000-222233334444',
    'dddd3333-eeee-ffff-0000-333344445555', 'eeee1111-ffff-0000-1111-222233334444',
    'eeee2222-ffff-0000-1111-333344445555', 'eeee3333-ffff-0000-1111-444455556666'
)
AND e."IsActive"
AND (
    extract(isodow FROM d.day) BETWEEN 1 AND 5
    OR (d.day = '2026-08-15'::date AND e."Id" IN (
        'd4e5f6a7-b8c9-0123-def0-234567890123',
        'bbbb2222-cccc-dddd-eeee-ffff22222222',
        'cccc1111-dddd-eeee-ffff-111122223333',
        'dddd1111-eeee-ffff-0000-111122223333',
        'eeee1111-ffff-0000-1111-222233334444'))
)
AND NOT (e."Id", d.day::date) IN (
    VALUES
        ('c3d4e5f6-a7b8-9012-cdef-123456789012'::uuid, '2026-08-12'::date),
        ('e5f6a7b8-c9d0-1234-ef01-345678901234', '2026-08-18'),
        ('cccc2222-dddd-eeee-ffff-222233334444', '2026-08-20'),
        ('dddd2222-eeee-ffff-0000-222233334444', '2026-08-21'),
        ('eeee2222-ffff-0000-1111-333344445555', '2026-08-25')
)
AND (
    NOT EXISTS (
        SELECT 1 FROM "Holidays" AS h
        JOIN demo_calendar AS c ON c.calendar_id = h."HolidayCalendarId"
        WHERE c."BranchId" = b."Id" AND h."Date" = d.day::date AND NOT h."IsOptional"
    )
    OR e."Id" IN (
        'aaaa2222-bbbb-cccc-dddd-eeee22222222',
        'bbbb3333-cccc-dddd-eeee-ffff33333333',
        'cccc1111-dddd-eeee-ffff-111122223333',
        'dddd1111-eeee-ffff-0000-111122223333',
        'eeee1111-ffff-0000-1111-222233334444'
    )
);

INSERT INTO "TimeKeepings" (
    "Id", "EmployeeId", "BranchId", "WorkDate", "TimeZoneId", "CreatedAt",
    "Latitude", "Longitude", "Source", "ConcurrencyToken"
)
SELECT s.timekeeping_id, s.employee_id, s.branch_id, s.work_date, s.time_zone,
       s.clock_in_local AT TIME ZONE s.time_zone,
       s.latitude, s.longitude, s.source,
       md5('demo-version:' || s.timekeeping_id::text)::uuid
FROM demo_shifts AS s
ON CONFLICT DO NOTHING;

INSERT INTO "TimeEntries" (
    "TimeKeepingId", "Timestamp", "Type", "Source", "Latitude", "Longitude",
    "AccuracyMeters", "CapturedAtUtc"
)
SELECT s.timekeeping_id, v.local_time AT TIME ZONE s.time_zone, v.entry_type, s.source,
       CASE WHEN s.source = 'Kiosk' THEN NULL ELSE s.latitude + 0.00003 END,
       CASE WHEN s.source = 'Kiosk' THEN NULL ELSE s.longitude - 0.00003 END,
       CASE WHEN s.source = 'Kiosk' THEN NULL ELSE 12.0 END,
       CASE WHEN s.source = 'Kiosk' THEN NULL ELSE v.local_time AT TIME ZONE s.time_zone END
FROM demo_shifts AS s
JOIN "TimeKeepings" AS tk ON tk."Id" = s.timekeeping_id
CROSS JOIN LATERAL (VALUES
    ('ClockIn', s.clock_in_local),
    ('BreakStart', s.break_start_local),
    ('BreakEnd', s.break_end_local),
    ('ClockOut', s.clock_out_local)
) AS v(entry_type, local_time)
WHERE NOT EXISTS (SELECT 1 FROM "TimeEntries" AS existing WHERE existing."TimeKeepingId" = s.timekeeping_id);

-- Approved, pending and rejected corrections exercise review and effective metrics.
INSERT INTO "TimeKeepingAdjustments" (
    "Id", "TimeKeepingId", "BranchId", "EmployeeId", "RequestedByEmployeeId",
    "ReviewedByEmployeeId", "EntryType", "OriginalTimestamp", "ProposedTimestamp",
    "Reason", "ReviewNote", "Status", "CreatedAt", "ReviewedAt", "ConcurrencyToken"
)
SELECT md5('demo-adjustment:' || v.employee_id::text || ':' || v.work_date::text)::uuid,
       tk."Id", tk."BranchId", tk."EmployeeId", v.employee_id,
       v.reviewer_id, v.entry_type, entry."Timestamp",
       entry."Timestamp" + v.delta,
       v.reason, v.review_note, v.status,
       '2026-09-01 12:00:00+00'::timestamptz,
       CASE WHEN v.reviewer_id IS NULL THEN NULL ELSE '2026-09-02 12:00:00+00'::timestamptz END,
       md5('demo-adjustment-version:' || tk."Id"::text)::uuid
FROM (VALUES
    ('c3d4e5f6-a7b8-9012-cdef-123456789012'::uuid, '2026-08-07'::date, 'ClockOut', interval '30 minutes', 'Approved', 'Saida registrada antes do horario real.', 'Ajuste confirmado pelo gestor.', 'aaaa1111-bbbb-cccc-dddd-eeee11111111'::uuid),
    ('d4e5f6a7-b8c9-0123-def0-234567890123', '2026-08-18', 'BreakEnd', interval '15 minutes', 'Pending', 'Retorno do intervalo registrado incorretamente.', NULL, NULL),
    ('cccc2222-dddd-eeee-ffff-222233334444', '2026-08-19', 'ClockIn', interval '30 minutes', 'Rejected', 'Solicito corrigir a entrada deste dia.', 'Comprovante nao corresponde ao horario.', '22222222-3333-4444-5555-666666666666')
) AS v(employee_id, work_date, entry_type, delta, status, reason, review_note, reviewer_id)
JOIN "TimeKeepings" AS tk ON tk."EmployeeId" = v.employee_id AND tk."WorkDate" = v.work_date
JOIN "TimeEntries" AS entry ON entry."TimeKeepingId" = tk."Id" AND entry."Type" = v.entry_type
ON CONFLICT ("Id") DO NOTHING;

COMMIT;

-- Summary for QA. Counts include only the August 2026 demo branch IDs.
SELECT b."CompanyCode", count(DISTINCT tk."Id") AS jornadas,
       count(DISTINCT tk."EmployeeId") AS pessoas_com_registro,
       count(te."Id") AS batidas,
       count(DISTINCT tk."Id") FILTER (WHERE h."Date" IS NOT NULL) AS jornadas_em_feriado
FROM "Branches" AS b
LEFT JOIN "TimeKeepings" AS tk ON tk."BranchId" = b."Id"
    AND tk."WorkDate" BETWEEN '2026-08-01' AND '2026-08-31'
LEFT JOIN "TimeEntries" AS te ON te."TimeKeepingId" = tk."Id"
LEFT JOIN "HolidayCalendars" AS hc ON hc."BranchId" = b."Id" AND hc."Year" = 2026 AND hc."IsActive"
LEFT JOIN "Holidays" AS h ON h."HolidayCalendarId" = hc."Id" AND h."Date" = tk."WorkDate"
WHERE b."Id" IN (
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'b2c3d4e5-f6a7-8901-bcde-f12345678901',
    '11111111-2222-3333-4444-555555555555',
    '11111111-2222-3333-4444-666666666666',
    '11111111-2222-3333-4444-777777777777'
)
GROUP BY b."CompanyCode"
ORDER BY b."CompanyCode";
