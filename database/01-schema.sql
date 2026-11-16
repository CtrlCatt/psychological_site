BEGIN;

CREATE TABLE assessments (
    id varchar(32) PRIMARY KEY CHECK (id ~ '^[a-z0-9]+(-[a-z0-9]+)*$'),
    title text NOT NULL,
    label text NOT NULL,
    description text NOT NULL,
    duration text NOT NULL,
    display_order integer NOT NULL UNIQUE CHECK (display_order > 0)
);

CREATE TABLE assessment_questions (
    assessment_id varchar(32) NOT NULL REFERENCES assessments(id),
    position integer NOT NULL CHECK (position >= 0),
    question_text text NOT NULL,
    sensitive boolean NOT NULL DEFAULT false,
    PRIMARY KEY (assessment_id, position)
);

CREATE TABLE assessment_options (
    assessment_id varchar(32) NOT NULL REFERENCES assessments(id),
    position integer NOT NULL CHECK (position >= 0),
    option_text text NOT NULL,
    PRIMARY KEY (assessment_id, position)
);

CREATE TABLE specialists (
    id varchar(32) PRIMARY KEY CHECK (id ~ '^[a-z0-9]+(-[a-z0-9]+)*$'),
    full_name text NOT NULL,
    professional_role text NOT NULL,
    experience_years integer NOT NULL CHECK (experience_years >= 0),
    price_rub integer NOT NULL CHECK (price_rub > 0),
    rating numeric(2,1) NOT NULL CHECK (rating BETWEEN 0 AND 5),
    image_url text NOT NULL,
    display_order integer NOT NULL UNIQUE CHECK (display_order > 0)
);

CREATE TABLE topics (
    id varchar(32) PRIMARY KEY,
    name text NOT NULL UNIQUE
);

CREATE TABLE specialist_topics (
    specialist_id varchar(32) NOT NULL REFERENCES specialists(id),
    topic_id varchar(32) NOT NULL REFERENCES topics(id),
    PRIMARY KEY (specialist_id, topic_id)
);

CREATE TABLE consultation_slots (
    id varchar(64) PRIMARY KEY CHECK (id ~ '^[a-z0-9]+(-[a-z0-9]+)*$'),
    specialist_id varchar(32) NOT NULL REFERENCES specialists(id),
    starts_at timestamptz NOT NULL,
    UNIQUE (specialist_id, starts_at)
);

CREATE TABLE assessment_results (
    id uuid PRIMARY KEY,
    visitor_id uuid NOT NULL,
    assessment_id varchar(32) NOT NULL REFERENCES assessments(id),
    score integer NOT NULL CHECK (score >= 0),
    needs_support boolean NOT NULL,
    completed_at timestamptz NOT NULL
);

CREATE TABLE bookings (
    id uuid PRIMARY KEY,
    visitor_id uuid NOT NULL,
    slot_id varchar(64) NOT NULL REFERENCES consultation_slots(id),
    CONSTRAINT uq_bookings_slot UNIQUE (slot_id)
);

CREATE INDEX ix_assessment_results_visitor_date ON assessment_results (visitor_id, completed_at DESC, id);
CREATE INDEX ix_assessment_results_assessment ON assessment_results (assessment_id);
CREATE INDEX ix_bookings_visitor ON bookings (visitor_id);
CREATE INDEX ix_specialist_topics_topic ON specialist_topics (topic_id);

COMMIT;
