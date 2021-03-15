﻿-- TABLE FOR MESSAGES TO BE SENT
CREATE TABLE MSG_QUEUE_TBL (
	MSG_ID UUID NOT NULL DEFAULT uuid_generate_v1(), -- UNIQUE IDENTIFIER FOR THE MESSAGE
	CRT_UTC TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP, -- CREATION TIME OF THE MESSAGE
	TO_ADDR VARCHAR(256) NOT NULL, -- THE PATIENT/ADDRESS WHO THE MESSAGE IS ADDRESSED TO
	SUB_TXT TEXT NOT NULL, -- TEXT IN THE SUBJECT
	BODY_TXT TEXT NOT NULL, -- TEXT IN THE BODY
	SCHDL_UTC DATE NOT NULL DEFAULT CURRENT_DATE, -- DATE WHEN THE MESSAGE SHOULD BE SENT
	SENT_UTC TIMESTAMPTZ, -- THE TIME THAT THE MESSAGE WAS SENT
	OUTC_CS VARCHAR(32), -- THE OUTCOME / STATUS TEXT
	CONSTRAINT PK_MSG_QUEUE_TBL PRIMARY KEY (MSG_ID)
);

CREATE TABLE APPT_SLOT_TBL (
	APPT_SLOT_ID UUID NOT NULL DEFAULT uuid_generate_v1(), -- UUID FOR APPT SLOT
	MSG_ID UUID NOT NULL, -- UNIQUE IDENTIFIER FOR THE MESSAGE
	PAT_ID UUID NOT NULL, -- PATIENT ID
	APPT_DT TIMESTAMP NOT NULL, -- SENT PROPOSAL TIME
	PLC_ID UUID NOT NULL, -- LOCATION
	CONSTRAINT PK_APPT_SLOT_TBL PRIMARY KEY (APPT_SLOT_ID), 
	CONSTRAINT FK_APPT_SLOT_MSG_TBL FOREIGN KEY (MSG_ID) REFERENCES MSG_QUEUE_TBL (MSG_ID),
	CONSTRAINT FK_APPT_SLOT_PAT_TBL FOREIGN KEY (PAT_ID) REFERENCES PAT_TBL(PAT_ID),
	CONSTRAINT FK_APPT_SLOT_PLC_TBL FOREIGN KEY (PLC_ID) REFERENCES FAC_TBL(FAC_ID)

);

CREATE TABLE MSG_QUEUE_LOG_TBL (
	LOG_ID UUID NOT NULL DEFAULT uuid_generate_v1(),
	MSG_ID UUID NOT NULL, -- THE MESSAGE ASSOCIATED
	PAT_ID UUID NOT NULL, -- THE PATIENT ASSOCIATED
	REF_ID UUID NOT NULL, -- ANY REFERENCE ID YOU LIKE
	PROC_NAME VARCHAR(32) NOT NULL, -- THE PROCESS THAT SENT THE MESSAGE
	CONSTRAINT PK_MSG_QUEUE_LOG_TBL PRIMARY KEY (LOG_ID),
	CONSTRAINT FK_MSG_QUEUE_PAT_TBL FOREIGN KEY (PAT_ID) REFERENCES PAT_TBL(PAT_ID),
	CONSTRAINT FK_MSG_QUEUE_MSG_TBL FOREIGN KEY (MSG_ID) REFERENCES MSG_QUEUE_TBL(MSG_ID)
);

CREATE INDEX MSG_QUEUE_LOG_REF_IDX ON MSG_QUEUE_LOG_TBL (REF_ID);

CREATE SEQUENCE REF_AGE_RG_SEQ START WITH 1 INCREMENT BY 1;

CREATE TABLE REF_AGE_RG_TBL (
	AGE_CAT_ID INT NOT NULL DEFAULT nextval('REF_AGE_RG_SEQ'),
	FROM_DAY INT NOT NULL,
	TO_DAY INT NOT NULL,
	AGE_NAME VARCHAR(32) NOT NULL,
	CONSTRAINT PK_AGE_CAT_ID PRIMARY KEY (AGE_CAT_ID),
	CONSTRAINT CK_AGE_DATE_RG CHECK (TO_DAY > FROM_DAY)
);

INSERT INTO REF_AGE_RG_TBL (FROM_DAY, TO_DAY, AGE_NAME) VALUES (0, 42, '< 6 WKS');
INSERT INTO REF_AGE_RG_TBL (FROM_DAY, TO_DAY, AGE_NAME) VALUES (42, 279, '6 WKS - 9 MO');
INSERT INTO REF_AGE_RG_TBL (FROM_DAY, TO_DAY, AGE_NAME) VALUES (279, 36500, '> 9 MO');

CREATE TABLE act_list_mat_tbl
(
	act_id UUID NOT NULL,
	mmat_id UUID NOT NULL,
	qty INT NOT NULL,
	vvm_cs VARCHAR(32),
	CONSTRAINT pk_act_list_mat_tbl PRIMARY KEY (act_id, mmat_id),
	CONSTRAINT fk_act_list_mat_list FOREIGN KEY (act_id) REFERENCES act_list_tbl(act_id),
	CONSTRAINT fk_act_list_mat_mat FOREIGN KEY (mmat_id) REFERENCES mmat_tbl(mmat_id)
);