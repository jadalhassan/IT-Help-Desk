-- ============================================================
-- IT Help Desk & Ticketing Management System
-- PostgreSQL Database Schema
--
-- Reference artifact:
-- The live ASP.NET Core application currently uses the EF Core model under
-- backend/Models and backend/Data/AppDbContext.cs as the executable schema.
-- This SQL file is preserved as a normalized PostgreSQL design/reference
-- artifact for database documentation and future migration planning.
-- ============================================================

DROP TABLE IF EXISTS activity_logs CASCADE;
DROP TABLE IF EXISTS notifications CASCADE;
DROP TABLE IF EXISTS ticket_attachments CASCADE;
DROP TABLE IF EXISTS ticket_comments CASCADE;
DROP TABLE IF EXISTS password_reset_tokens CASCADE;
DROP TABLE IF EXISTS knowledge_base_articles CASCADE;
DROP TABLE IF EXISTS sla_rules CASCADE;
DROP TABLE IF EXISTS tickets CASCADE;
DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS roles CASCADE;
DROP TABLE IF EXISTS categories CASCADE;
DROP TABLE IF EXISTS priorities CASCADE;
DROP TABLE IF EXISTS statuses CASCADE;

-- ============================================================
-- ROLES TABLE
-- ============================================================

CREATE TABLE roles (
    role_id SERIAL PRIMARY KEY,
    role_name VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- USERS TABLE
-- ============================================================

CREATE TABLE users (
    user_id SERIAL PRIMARY KEY,
    role_id INT NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    phone_number VARCHAR(30),
    department VARCHAR(100),
    job_title VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_users_role
        FOREIGN KEY (role_id)
        REFERENCES roles(role_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

-- ============================================================
-- CATEGORIES TABLE
-- ============================================================

CREATE TABLE categories (
    category_id SERIAL PRIMARY KEY,
    category_name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- PRIORITIES TABLE
-- ============================================================

CREATE TABLE priorities (
    priority_id SERIAL PRIMARY KEY,
    priority_name VARCHAR(50) NOT NULL UNIQUE,
    priority_level INT NOT NULL UNIQUE,
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_priority_level
        CHECK (priority_level > 0)
);

-- ============================================================
-- STATUSES TABLE
-- ============================================================

CREATE TABLE statuses (
    status_id SERIAL PRIMARY KEY,
    status_name VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    is_final_status BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- TICKETS TABLE
-- ============================================================

CREATE TABLE tickets (
    ticket_id SERIAL PRIMARY KEY,
    ticket_reference VARCHAR(30) NOT NULL UNIQUE,

    employee_id INT NOT NULL,
    assigned_agent_id INT,

    category_id INT NOT NULL,
    priority_id INT NOT NULL,
    status_id INT NOT NULL,

    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assigned_at TIMESTAMP,
    resolved_at TIMESTAMP,
    closed_at TIMESTAMP,

    CONSTRAINT fk_tickets_employee
        FOREIGN KEY (employee_id)
        REFERENCES users(user_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT fk_tickets_assigned_agent
        FOREIGN KEY (assigned_agent_id)
        REFERENCES users(user_id)
        ON UPDATE CASCADE
        ON DELETE SET NULL,

    CONSTRAINT fk_tickets_category
        FOREIGN KEY (category_id)
        REFERENCES categories(category_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT fk_tickets_priority
        FOREIGN KEY (priority_id)
        REFERENCES priorities(priority_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT fk_tickets_status
        FOREIGN KEY (status_id)
        REFERENCES statuses(status_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

-- ============================================================
-- TICKET COMMENTS TABLE
-- ============================================================

CREATE TABLE ticket_comments (
    comment_id SERIAL PRIMARY KEY,
    ticket_id INT NOT NULL,
    user_id INT NOT NULL,
    comment_text TEXT NOT NULL,
    is_internal_note BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_ticket_comments_ticket
        FOREIGN KEY (ticket_id)
        REFERENCES tickets(ticket_id)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT fk_ticket_comments_user
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

-- ============================================================
-- TICKET ATTACHMENTS TABLE
-- ============================================================

CREATE TABLE ticket_attachments (
    attachment_id SERIAL PRIMARY KEY,
    ticket_id INT NOT NULL,
    uploaded_by INT NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_type VARCHAR(100) NOT NULL,
    file_path TEXT NOT NULL,
    file_size INT NOT NULL,
    uploaded_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_file_size
        CHECK (file_size > 0),

    CONSTRAINT fk_ticket_attachments_ticket
        FOREIGN KEY (ticket_id)
        REFERENCES tickets(ticket_id)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT fk_ticket_attachments_uploaded_by
        FOREIGN KEY (uploaded_by)
        REFERENCES users(user_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

-- ============================================================
-- NOTIFICATIONS TABLE
-- ============================================================

CREATE TABLE notifications (
    notification_id SERIAL PRIMARY KEY,
    user_id INT NOT NULL,
    ticket_id INT,
    notification_type VARCHAR(80) NOT NULL,
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    read_at TIMESTAMP,

    CONSTRAINT fk_notifications_user
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT fk_notifications_ticket
        FOREIGN KEY (ticket_id)
        REFERENCES tickets(ticket_id)
        ON UPDATE CASCADE
        ON DELETE CASCADE
);

-- ============================================================
-- ACTIVITY LOGS TABLE
-- ============================================================

CREATE TABLE activity_logs (
    activity_log_id SERIAL PRIMARY KEY,
    ticket_id INT,
    user_id INT NOT NULL,
    action_type VARCHAR(100) NOT NULL,
    action_description TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_activity_logs_ticket
        FOREIGN KEY (ticket_id)
        REFERENCES tickets(ticket_id)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT fk_activity_logs_user
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

-- ============================================================
-- PASSWORD RESET TOKENS TABLE
-- ============================================================

CREATE TABLE password_reset_tokens (
    token_id SERIAL PRIMARY KEY,
    user_id INT NOT NULL,
    token TEXT NOT NULL UNIQUE,
    expires_at TIMESTAMP NOT NULL,
    is_used BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_password_reset_tokens_user
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON UPDATE CASCADE
        ON DELETE CASCADE
);

-- ============================================================
-- KNOWLEDGE BASE ARTICLES TABLE
-- Optional advanced module
-- ============================================================

CREATE TABLE knowledge_base_articles (
    article_id SERIAL PRIMARY KEY,
    category_id INT NOT NULL,
    created_by INT NOT NULL,
    title VARCHAR(200) NOT NULL,
    content TEXT NOT NULL,
    is_approved BOOLEAN NOT NULL DEFAULT FALSE,
    is_published BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_knowledge_base_articles_category
        FOREIGN KEY (category_id)
        REFERENCES categories(category_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT fk_knowledge_base_articles_created_by
        FOREIGN KEY (created_by)
        REFERENCES users(user_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

-- ============================================================
-- SLA RULES TABLE
-- Optional advanced module
-- ============================================================

CREATE TABLE sla_rules (
    sla_rule_id SERIAL PRIMARY KEY,
    priority_id INT NOT NULL UNIQUE,
    response_time_hours INT NOT NULL,
    resolution_time_hours INT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_response_time
        CHECK (response_time_hours > 0),

    CONSTRAINT chk_resolution_time
        CHECK (resolution_time_hours > 0),

    CONSTRAINT fk_sla_rules_priority
        FOREIGN KEY (priority_id)
        REFERENCES priorities(priority_id)
        ON UPDATE CASCADE
        ON DELETE CASCADE
);

-- ============================================================
-- INDEXES
-- ============================================================

CREATE INDEX idx_users_role_id ON users(role_id);
CREATE INDEX idx_users_email ON users(email);

CREATE INDEX idx_tickets_employee_id ON tickets(employee_id);
CREATE INDEX idx_tickets_assigned_agent_id ON tickets(assigned_agent_id);
CREATE INDEX idx_tickets_category_id ON tickets(category_id);
CREATE INDEX idx_tickets_priority_id ON tickets(priority_id);
CREATE INDEX idx_tickets_status_id ON tickets(status_id);
CREATE INDEX idx_tickets_reference ON tickets(ticket_reference);
CREATE INDEX idx_tickets_created_at ON tickets(created_at);

CREATE INDEX idx_ticket_comments_ticket_id ON ticket_comments(ticket_id);
CREATE INDEX idx_ticket_comments_user_id ON ticket_comments(user_id);

CREATE INDEX idx_ticket_attachments_ticket_id ON ticket_attachments(ticket_id);

CREATE INDEX idx_notifications_user_id ON notifications(user_id);
CREATE INDEX idx_notifications_ticket_id ON notifications(ticket_id);
CREATE INDEX idx_notifications_is_read ON notifications(is_read);

CREATE INDEX idx_activity_logs_ticket_id ON activity_logs(ticket_id);
CREATE INDEX idx_activity_logs_user_id ON activity_logs(user_id);

-- ============================================================
-- SEED DATA
-- ============================================================

INSERT INTO roles (role_name, description) VALUES
('Admin', 'Full system access'),
('IT Support Agent', 'Can manage and resolve assigned tickets'),
('Employee', 'Can create and track support tickets'),
('Manager', 'Can monitor team tickets and reports');

INSERT INTO categories (category_name, description) VALUES
('Hardware', 'Computer, laptop, printer, and device issues'),
('Software', 'Application and software-related issues'),
('Network', 'Internet, Wi-Fi, VPN, and connectivity issues'),
('Email', 'Email access, password, and configuration issues'),
('Access Request', 'Requests for system or data access'),
('Other', 'General IT support requests');

INSERT INTO priorities (priority_name, priority_level, description) VALUES
('Low', 1, 'Minor issue with low business impact'),
('Medium', 2, 'Normal issue requiring support'),
('High', 3, 'Important issue affecting work'),
('Critical', 4, 'Urgent issue affecting major operations');

INSERT INTO statuses (status_name, description, is_final_status) VALUES
('Open', 'Ticket has been created and is waiting for action', FALSE),
('In Progress', 'Ticket is currently being handled by an agent', FALSE),
('Pending', 'Ticket is waiting for user response or external action', FALSE),
('Resolved', 'Ticket issue has been solved', FALSE),
('Closed', 'Ticket has been confirmed and closed', TRUE);

INSERT INTO sla_rules (priority_id, response_time_hours, resolution_time_hours) VALUES
(1, 24, 72),
(2, 8, 48),
(3, 4, 24),
(4, 1, 8);
