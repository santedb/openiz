﻿CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS TABLEFUNC;

-- REPRESENTS A SINGLE EXECUTION OF AN ETL EXECUTION
CREATE TABLE WHSE_ETL_TBL (
	ETL_ID UUID NOT NULL DEFAULT uuid_generate_v4(),
	START_UTC TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP, -- TIME THE PROCESS STARTED
	STOP_UTC TIMESTAMPTZ, -- THE TIME THE PROCESS STOPPED
	CONSTRAINT PK_WHSE_ETL_TBL PRIMARY KEY (ETL_ID)
);

-- CODE TABLE
CREATE TABLE CD_TBL (
	SET_MNEMONIC TEXT, -- MNEMONIC OF THE SET
	CD_MNEMONIC TEXT, -- MNEMONIC OF THE SET
	DISPLAY TEXT, -- DISPLAY NAME OF CODE
	CONSTRAINT PK_CD_TBL PRIMARY KEY (CD_MNEMONIC, SET_MNEMONIC)
);

-- ADDRESS COPY OF PRODUCTION TABLE
CREATE TABLE ADDR_CMP_TBL (
	ADDR_CMP_ID UUID NOT NULL DEFAULT uuid_generate_v4(),
	ADDR_ID UUID NOT NULL, -- UUID OF THE ADDRESS
	ENT_ID	UUID NOT NULL, -- UUID OF THE ENTITY (PLACE, PERSON, PATIENT)
	USE_CS	TEXT NOT NULL, -- USE OF THE ADDRESS
	TYP	TEXT NOT NULL, -- TYPE OF ADDRESS COMPONNT
	VALUE	TEXT NOT NULL, -- VALUE OF THE ADDRESS COMPONENT
	CONSTRAINT PK_ADDR_CMP_TBL PRIMARY KEY (ADDR_CMP_ID)
);

-- ADDRESS COPY OF PRODUCTION TABLE
CREATE TABLE NAME_CMP_TBL (
	NAME_CMP_ID UUID NOT NULL DEFAULT uuid_generate_v4(),
	NAME_ID UUID NOT NULL, -- UUID OF THE NAME
	ENT_ID	UUID NOT NULL, -- UUID OF THE ENTITY (PLACE, PERSON, PATIENT)
	USE_CS	TEXT NOT NULL, -- USE OF THE NAME
	TYP	TEXT NOT NULL, -- TYPE OF NAME COMPONNT
	VALUE	TEXT NOT NULL, -- VALUE OF THE NAME COMPONENT
	CONSTRAINT PK_NAME_CMP_TBL PRIMARY KEY (NAME_CMP_ID)
);


-- PLACE INFORMATION TABLE
CREATE TABLE PLC_TBL (
	PLC_ID UUID NOT NULL, 
	CRT_ETL_ID UUID NOT NULL, -- LINK TO THE ETL PROCESS WHICH WAS FIRST USED TO EXTRACT THIS DATA
	UPD_ETL_ID UUID, -- THE ETL PROCESS WHICH REPRESENTS THE CURRENT TUPLE IN THE WAREHOUSE
	CRT_UTC TIMESTAMPTZ NOT NULL, -- THE TIME THAT THE RECORD WAS CREATED IN THE ORIGINAL TABLE
	UPD_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS UPDATED IN THE ORIGINAL TABLE
	OBSLT_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS OBSOLETED IN THE ORIGINAL TABLE
	STS_CS TEXT, -- THE STATUS OF THE PERSON
	TYPE_MNEMONIC VARCHAR(32), -- TYPE OF PLACE (DISTRICT, CITY, ETC.)
	PARENT_ID UUID, -- THE ID OF THE PARENT PLACE
	CONSTRAINT PK_PLC_TBL PRIMARY KEY (PLC_ID),
	CONSTRAINT FK_PLC_CRT_WHSE_ETL FOREIGN KEY (CRT_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_PLC_UPD_WHSE_ETL FOREIGN KEY (UPD_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_PLC_PARENT_ID FOREIGN KEY (PARENT_ID) REFERENCES PLC_TBL(PLC_ID)
);

-- FACILITY INFORMATION TABLE
CREATE TABLE FAC_TBL (
	FAC_ID UUID NOT NULL, 
	CRT_ETL_ID UUID NOT NULL, -- LINK TO THE ETL PROCESS WHICH WAS FIRST USED TO EXTRACT THIS DATA
	UPD_ETL_ID UUID, -- THE ETL PROCESS WHICH REPRESENTS THE CURRENT TUPLE IN THE WAREHOUSE
	CRT_UTC TIMESTAMPTZ NOT NULL, -- THE TIME THAT THE RECORD WAS CREATED IN THE ORIGINAL TABLE
	UPD_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS UPDATED IN THE ORIGINAL TABLE
	OBSLT_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS OBSOLETED IN THE ORIGINAL TABLE
	STS_CS TEXT, -- THE STATUS OF THE PERSON
	TYPE_MNEMONIC VARCHAR(32), -- THE TYPE OF FACILITY
	PARENT_ID UUID, -- THE PARENT FACILITY OR ADMINISTRATION POINT
	TEL VARCHAR(32),
	CONSTRAINT PK_FAC_TBL PRIMARY KEY (FAC_ID),
	CONSTRAINT FK_FAC_CRT_WHSE_ETL FOREIGN KEY (CRT_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_FAC_UPD_WHSE_ETL FOREIGN KEY (UPD_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_FAC_PARENT_ID FOREIGN KEY (PARENT_ID) REFERENCES FAC_TBL(FAC_ID)
);

-- FACILITY <> PLACE SERVICE LINK
CREATE TABLE FAC_PLC_DED_SDL_TBL (
	FAC_ID UUID NOT NULL,
	PLC_ID UUID NOT NULL,
	CRT_ETL_ID UUID NOT NULL, -- LINK TO THE ETL PROCESS WHICH WAS FIRST USED TO EXTRACT THIS DATA
	UPD_ETL_ID UUID, -- THE ETL PROCESS WHICH REPRESENTS THE CURRENT TUPLE IN THE WAREHOUSE
	CRT_UTC TIMESTAMPTZ NOT NULL, -- THE TIME THAT THE RECORD WAS CREATED IN THE ORIGINAL TABLE
	UPD_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS UPDATED IN THE ORIGINAL TABLE
	OBSLT_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS OBSOLETED IN THE ORIGINAL TABLE
	CONSTRAINT PK_FAC_PLC_DED_SDL_TBL PRIMARY KEY (FAC_ID, PLC_ID),
	CONSTRAINT FK_FAC_PLC_DED_SDL_CRT_WHSE_ETL FOREIGN KEY (CRT_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_FAC_PLC_DED_SDL_UPD_WHSE_ETL FOREIGN KEY (UPD_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_FAC_PLC_DED_SDL_FAC_ID FOREIGN KEY (FAC_ID) REFERENCES FAC_TBL(FAC_ID),
	CONSTRAINT FK_FAC_PLC_DED_SDL_PLC_ID FOREIGN KEY (PLC_ID) REFERENCES PLC_TBL(PLC_ID)
);

-- PERSON (NON PATIENT) INFORMATION
CREATE TABLE PSN_TBL (
	PSN_ID UUID NOT NULL,
	CRT_ETL_ID UUID NOT NULL, -- LINK TO THE ETL PROCESS WHICH WAS FIRST USED TO EXTRACT THIS DATA
	UPD_ETL_ID UUID, -- THE ETL PROCESS WHICH REPRESENTS THE CURRENT TUPLE IN THE WAREHOUSE
	CRT_UTC TIMESTAMPTZ NOT NULL, -- THE TIME THAT THE RECORD WAS CREATED IN THE ORIGINAL TABLE
	UPD_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS UPDATED IN THE ORIGINAL TABLE
	OBSLT_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS OBSOLETED IN THE ORIGINAL TABLE
	STS_CS TEXT NOT NULL, -- THE STATUS OF THE PERSON
	DOB DATE,
	ALT_ID VARCHAR(32), -- AN ALTERNATE IDENTIFIER FOR THE PATIENT
	ALT_ID_TYPE VARCHAR(32), -- THE TYPE OF THE ALTERNATE IDENTIFIER
	TEL VARCHAR(256), -- TELECOM
	CONSTRAINT PK_PSN_TBL PRIMARY KEY (PSN_ID),
	CONSTRAINT FK_PSN_CRT_WHSE_ETL FOREIGN KEY (CRT_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_PSN_UPD_WHSE_ETL FOREIGN KEY (UPD_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID)
);


-- PROVIDER TABLE
CREATE TABLE PVD_TBL (
	PVD_ID UUID NOT NULL, 
	CRT_ETL_ID UUID NOT NULL, -- LINK TO THE ETL PROCESS WHICH WAS FIRST USED TO EXTRACT THIS DATA
	UPD_ETL_ID UUID, -- THE ETL PROCESS WHICH REPRESENTS THE CURRENT TUPLE IN THE WAREHOUSE
	CRT_UTC TIMESTAMPTZ NOT NULL, -- THE TIME THAT THE RECORD WAS CREATED IN THE ORIGINAL TABLE
	UPD_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS UPDATED IN THE ORIGINAL TABLE
	OBSLT_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS OBSOLETED IN THE ORIGINAL TABLE
	PSN_ID UUID NOT NULL, -- PERSON INFORMATION RELATED TO THIS PROVIDER
	USR_NAME VARCHAR(128), -- THE USER IDENTIFIER OF THIS PROVIDER
	EMAIL VARCHAR(256), -- THE EMAIL ADDRESS OF THE PROVIDER
	FAC_ID UUID, -- THE FACILITY THIS PROVIDER IS ASSIGNED TO
	CONSTRAINT PK_PVD_TBL PRIMARY KEY (PVD_ID),
	CONSTRAINT FK_PVD_CRT_WHSE_ETL FOREIGN KEY (CRT_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_PVD_UPD_WHSE_ETL FOREIGN KEY (UPD_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_PVD_PSN_ID FOREIGN KEY (PSN_ID) REFERENCES PSN_TBL(PSN_ID),
	CONSTRAINT FK_PVD_FAC_ID FOREIGN KEY (FAC_ID) REFERENCES FAC_TBL(FAC_ID)
);

-- PATIENT TABLE
CREATE TABLE PAT_TBL (
	PAT_ID UUID NOT NULL,
	PSN_ID UUID NOT NULL, -- THE PERSON INFORMATION FOR THIS PATIENT
	MTH_ID UUID, -- THE PERSON WHICH IS THE MOTHER OF THIS PATIENT
	NOK_ID UUID, -- THE PERSON WHICH IS THE NEXT OF KIN FOR THIS PATIENT
	NOK_TYP_MNEMONIC VARCHAR(32), -- THE MNEMONIC OF THE NEXT OF KIN RELATIONSHIP
	REG_FAC_ID UUID, -- THE BIRTH REGISTRATION FACILITY
	ASGN_FAC_ID UUID NOT NULL, -- THE ASGN FACILITY IDENTIFIER
	DECEASED DATE, -- DATE THE CHILD DIED
	GENDER_MNEMONIC VARCHAR(32),
	CONSTRAINT PK_PAT_TBL PRIMARY KEY (PAT_ID),
	CONSTRAINT FK_PAT_PSN_ID FOREIGN KEY (PSN_ID) REFERENCES PSN_TBL(PSN_ID),
	CONSTRAINT FK_PAT_MTH_ID FOREIGN KEY (MTH_ID) REFERENCES PSN_TBL(PSN_ID),
	CONSTRAINT FK_PAT_NOK_ID FOREIGN KEY (NOK_ID) REFERENCES PSN_TBL(PSN_ID),
	CONSTRAINT FK_PAT_REG_FAC_ID FOREIGN KEY (REG_FAC_ID) REFERENCES FAC_TBL(FAC_ID),
	CONSTRAINT FK_PAT_ASSGN_FAC_ID FOREIGN KEY (ASGN_FAC_ID) REFERENCES FAC_TBL(FAC_ID)
);

-- NO MORE THAN ONE PATIENT CAN BE ASGN TO A PERSON
CREATE UNIQUE INDEX PAT_PSN_ID_IDX ON PAT_TBL(PSN_ID);
CREATE INDEX PAT_PLC_ID_IDX ON PAT_TBL(PLC_ID);
CREATE INDEX PAT_ASGN_FAC_ID_IDX ON PAT_TBL(ASGN_FAC_ID);

-- MATERIALS TABLE
CREATE TABLE MAT_TBL (
	MAT_ID UUID NOT NULL, 
	CRT_ETL_ID UUID NOT NULL, -- LINK TO THE ETL PROCESS WHICH WAS FIRST USED TO EXTRACT THIS DATA
	UPD_ETL_ID UUID, -- THE ETL PROCESS WHICH REPRESENTS THE CURRENT TUPLE IN THE WAREHOUSE
	CRT_UTC TIMESTAMPTZ NOT NULL, -- THE TIME THAT THE RECORD WAS CREATED IN THE ORIGINAL TABLE
	UPD_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS UPDATED IN THE ORIGINAL TABLE
	OBSLT_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS OBSOLETED IN THE ORIGINAL TABLE
	TYPE_MNEMONIC VARCHAR(32), -- THE TYPE OF MATERIAL
	DOSE_UNIT VARCHAR(32),
	FORM_CODE VARCHAR(32),
	CONSTRAINT PK_MAT_TBL PRIMARY KEY (MAT_ID),
	CONSTRAINT FK_MAT_CRT_WHSE_ETL FOREIGN KEY (CRT_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_MAT_UPD_WHSE_ETL FOREIGN KEY (UPD_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID)
);

-- MANUFACTURED MATERIALS TABLE
CREATE TABLE MMAT_TBL (
	MMAT_ID UUID NOT NULL, 
	MAT_ID UUID, -- THE MATERIAL WHICH THIS MMAT "IS A"
	CRT_ETL_ID UUID NOT NULL, -- LINK TO THE ETL PROCESS WHICH WAS FIRST USED TO EXTRACT THIS DATA
	UPD_ETL_ID UUID, -- THE ETL PROCESS WHICH REPRESENTS THE CURRENT TUPLE IN THE WAREHOUSE
	CRT_UTC TIMESTAMPTZ NOT NULL, -- THE TIME THAT THE RECORD WAS CREATED IN THE ORIGINAL TABLE
	UPD_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS UPDATED IN THE ORIGINAL TABLE
	OBSLT_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS OBSOLETED IN THE ORIGINAL TABLE
	TYPE_MNEMONIC VARCHAR(32), -- THE TYPE OF MATERIAL
	EXPIRY DATE,
	GTIN VARCHAR(64),
	LOT VARCHAR(32),
	MANUFACTURER VARCHAR(48),
	CONSTRAINT PK_MMAT_TBL PRIMARY KEY (MMAT_ID),
	CONSTRAINT FK_MMAT_CRT_WHSE_ETL FOREIGN KEY (CRT_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_MMAT_UPD_WHSE_ETL FOREIGN KEY (UPD_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_MMAT_MAT_ID FOREIGN KEY (MAT_ID) REFERENCES MAT_TBL(MAT_ID)
);

-- FACILITY TO MATERIAL LEDGER TABLE
CREATE TABLE FAC_MAT_LDGR_TBL (
	FAC_ID UUID NOT NULL,
	MMAT_ID UUID NOT NULL, 
	CRT_ETL_ID UUID NOT NULL, -- LINK TO THE ETL PROCESS WHICH WAS FIRST USED TO EXTRACT THIS DATA
	UPD_ETL_ID UUID, -- THE ETL PROCESS WHICH REPRESENTS THE CURRENT TUPLE IN THE WAREHOUSE
	CRT_UTC TIMESTAMPTZ NOT NULL, -- THE TIME THAT THE RECORD WAS CREATED IN THE ORIGINAL TABLE
	UPD_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS UPDATED IN THE ORIGINAL TABLE
	OBSLT_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS OBSOLETED IN THE ORIGINAL TABLE
	REF_ID UUID, -- REFERENCE OF THE LEDGER OPERATION
	BAL_EOL NUMERIC(20,0) NOT NULL DEFAULT 0, -- BALANCE AT THIS LINE ITEM
	QTY NUMERIC(20,0) NOT NULL, -- QUANTITY THAT WAS ADDED-SUBTRACTED
	TX_DESC VARCHAR(32) NOT NULL, -- TRANSACTION DESCRIPTION
	USR_NAME VARCHAR(128) NOT NULL, -- THE USER WHICH DID THE ACTION
	CONSTRAINT PK_FAC_MAT_LDGR_TBL PRIMARY KEY (FAC_ID, MMAT_ID),
	CONSTRAINT FK_FAC_MAT_LDGR_FAC_ID FOREIGN KEY (FAC_ID) REFERENCES FAC_TBL(FAC_ID),
	CONSTRAINT FK_FAC_MAT_LDGR_MMAT_ID FOREIGN KEY (MMAT_ID) REFERENCES MMAT_TBL(MMAT_ID),
	CONSTRAINT FK_FAC_MMAT_LDGR_CRT_WHSE_ETL FOREIGN KEY (CRT_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_FAC_MMAT_LDGR_UPD_WHSE_ETL FOREIGN KEY (UPD_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID)
);

-- ENCOUNTER TABLE
CREATE TABLE ENC_TBL (
	ENC_ID UUID NOT NULL,
	CRT_ETL_ID UUID NOT NULL, -- LINK TO THE ETL PROCESS WHICH WAS FIRST USED TO EXTRACT THIS DATA
	UPD_ETL_ID UUID, -- THE ETL PROCESS WHICH REPRESENTS THE CURRENT TUPLE IN THE WAREHOUSE
	CRT_UTC TIMESTAMPTZ NOT NULL, -- THE TIME THAT THE RECORD WAS CREATED IN THE ORIGINAL TABLE
	UPD_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS UPDATED IN THE ORIGINAL TABLE
	OBSLT_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS OBSOLETED IN THE ORIGINAL TABLE
	START_UTC TIMESTAMPTZ, -- THE START OF THE ENCOUNTER
	STOP_UTC TIMESTAMPTZ, -- THE STOP TIME OF THE ENCOUNTER
	ACT_UTC TIMESTAMPTZ, -- THE TIME THE ENCOUNTER OCCURRED
	PVD_ID UUID NOT NULL, -- THE PROVIDER DOING THE ENCOUNTER
	PAT_ID UUID NOT NULL, -- THE PATIENT TO WHICH THE DATA APPLIES
	FAC_ID UUID NOT NULL, -- FACILITY WHERE THE DATA WAS ENTERED
	DISCHARGE_CS  VARCHAR(32), -- DISCHARGE DISPOSITION
	CONSTRAINT PK_ENC_TBL PRIMARY KEY (ENC_ID),
	CONSTRAINT FK_ENC_CRT_WHSE_ETL FOREIGN KEY (CRT_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_ENC_UPD_WHSE_ETL FOREIGN KEY (UPD_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_ENC_PVD_ID FOREIGN KEY (PVD_ID) REFERENCES PVD_TBL(PVD_ID),
	CONSTRAINT FK_ENC_PAT_ID FOREIGN KEY (PAT_ID) REFERENCES PAT_TBL(PAT_ID),
	CONSTRAINT FK_ENC_FAC_ID FOREIGN KEY (FAC_ID) REFERENCES FAC_TBL(FAC_ID)
);


-- SUBSTANCE ADMINISTRATION TABLE
CREATE TABLE SBADM_TBL (
	ACT_ID UUID NOT NULL, 
	ENC_ID UUID,
	CRT_ETL_ID UUID NOT NULL, -- LINK TO THE ETL PROCESS WHICH WAS FIRST USED TO EXTRACT THIS DATA
	UPD_ETL_ID UUID, -- THE ETL PROCESS WHICH REPRESENTS THE CURRENT TUPLE IN THE WAREHOUSE
	CRT_UTC TIMESTAMPTZ NOT NULL, -- THE TIME THAT THE RECORD WAS CREATED IN THE ORIGINAL TABLE
	UPD_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS UPDATED IN THE ORIGINAL TABLE
	OBSLT_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS OBSOLETED IN THE ORIGINAL TABLE
	PROP_UTC TIMESTAMPTZ, -- THE PROPOSED TIME THAT THE ACT WAS SUPPOSED TO OCCUR
	ACT_UTC TIMESTAMPTZ NOT NULL, -- THE TIME THAT THE ACT WAS DONE
	NEG_IND BOOLEAN NOT NULL DEFAULT FALSE,
	RSN_MNEMONIC VARCHAR(32), -- REASON
	SEQ_ID INT, -- THE DOSE SEQUENCE
	PVD_ID UUID, -- THE PROVIDER WHICH DID THE VACCINATION IF KNOWN
	MAT_ID UUID NOT NULL, -- MATERIAL FOR THE VACCINATION
	MMAT_ID UUID, -- THE MATERIAL FOR THE VACCINATION IF KNOWN
	SITE_MNEMONIC VARCHAR(32), -- THE TARGET SITE OF ADMINISTRATION
	TYPE_MNEMONIC VARCHAR(32), -- THE CLASS/TYPE OF ADMINISTRATION
	ROUT_MNEMONIC VARCHAR(32), -- THE ROUTE OF ADMINISTRATION
	PAT_ID UUID NOT NULL, -- THE PATIENT TO WHICH THE DATA APPLIES
	FAC_ID UUID, -- FACILITY WHERE THE DATA WAS ENTERED IF KNOWN
	CONSTRAINT PK_SBADM_TBL PRIMARY KEY (ACT_ID),
	CONSTRAINT FK_SBADM_CRT_WHSE_ETL FOREIGN KEY (CRT_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_SBADM_UPD_WHSE_ETL FOREIGN KEY (UPD_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_SBADM_PVD_ID FOREIGN KEY (PVD_ID) REFERENCES PVD_TBL(PVD_ID),
	CONSTRAINT FK_SBADM_MMAT_ID FOREIGN KEY (MMAT_ID) REFERENCES MMAT_TBL(MMAT_ID),
	CONSTRAINT FK_SBADM_ENC_ID FOREIGN KEY (ENC_ID) REFERENCES ENC_TBL(ENC_ID),
	CONSTRAINT FK_SBADM_PAT_ID FOREIGN KEY (PAT_ID) REFERENCES PAT_TBL(PAT_ID),
	CONSTRAINT FK_SBADM_FAC_ID FOREIGN KEY (FAC_ID) REFERENCES FAC_TBL(FAC_ID)
);

-- OBSERVATIONS TABLE FOR QUANTITY
CREATE TABLE QTY_OBS_TBL (
	ACT_ID UUID NOT NULL,
	ENC_ID UUID,
	CRT_ETL_ID UUID NOT NULL, -- LINK TO THE ETL PROCESS WHICH WAS FIRST USED TO EXTRACT THIS DATA
	UPD_ETL_ID UUID, -- THE ETL PROCESS WHICH REPRESENTS THE CURRENT TUPLE IN THE WAREHOUSE
	CRT_UTC TIMESTAMPTZ NOT NULL, -- THE TIME THAT THE RECORD WAS CREATED IN THE ORIGINAL TABLE
	UPD_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS UPDATED IN THE ORIGINAL TABLE
	OBSLT_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS OBSOLETED IN THE ORIGINAL TABLE
	ACT_UTC TIMESTAMPTZ NOT NULL, -- TIME THE ACTION TOOK PLACE
	TYP_CS VARCHAR(32) NOT NULL, -- TYPE OF OBSERVATION MADE
	INT_CS VARCHAR(32), -- THE INTERPRETATION
	PVD_ID UUID, -- THE PROVIDER WHICH DID THE VACCINATION
	QTY NUMERIC(20,10) NOT NULL, -- OBSERVED VALUE
	UOM VARCHAR(32) NOT NULL, -- UNITS OF MEAURE
	PAT_ID UUID NOT NULL, -- THE PATIENT TO WHICH THE DATA APPLIES
	FAC_ID UUID, -- FACILITY WHERE THE DATA WAS ENTERED
	CONSTRAINT PK_QTY_OBS_TBL PRIMARY KEY (ACT_ID) ,
	CONSTRAINT FK_QTY_OBS_CRT_WHSE_ETL FOREIGN KEY (CRT_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_QTY_OBS_UPD_WHSE_ETL FOREIGN KEY (UPD_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_QTY_OBS_PVD_ID FOREIGN KEY (PVD_ID) REFERENCES PVD_TBL(PVD_ID),
	CONSTRAINT FK_QTY_OBS_ENC_ID FOREIGN KEY (ENC_ID) REFERENCES ENC_TBL(ENC_ID),
	CONSTRAINT FK_QTY_OBS_PAT_ID FOREIGN KEY (PAT_ID) REFERENCES PAT_TBL(PAT_ID),
	CONSTRAINT FK_QTY_OBS_FAC_ID FOREIGN KEY (FAC_ID) REFERENCES FAC_TBL(FAC_ID)
);

-- AEFI TABLE
CREATE TABLE AEFI_TBL (
	ACT_ID UUID NOT NULL,
	ENC_ID UUID,
	CRT_ETL_ID UUID NOT NULL, -- LINK TO THE ETL PROCESS WHICH WAS FIRST USED TO EXTRACT THIS DATA
	UPD_ETL_ID UUID, -- THE ETL PROCESS WHICH REPRESENTS THE CURRENT TUPLE IN THE WAREHOUSE
	CRT_UTC TIMESTAMPTZ NOT NULL, -- THE TIME THAT THE RECORD WAS CREATED IN THE ORIGINAL TABLE
	UPD_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS UPDATED IN THE ORIGINAL TABLE
	OBSLT_UTC TIMESTAMPTZ, -- THE TIME THAT THE RECORD WAS OBSOLETED IN THE ORIGINAL TABLE
	PVD_ID UUID NOT NULL, -- THE PROVIDER WHICH DID THE VACCINATION
	MMAT_ID UUID, -- THE MATERIAL 
	RCTN_MNEMONIC VARCHAR(32), -- THE REACTION
	SEV_MNEMONIC VARCHAR(32), -- THE SEVERITY
	START DATE, -- DATE REACTION STARTED
	STOP DATE, -- DATE REACTION STOPPED
	CONCERN_IND BOOLEAN, -- TRUE IF IT IS AN ONGOING CONCERN
	PAT_ID UUID NOT NULL, -- THE PATIENT TO WHICH THE DATA APPLIES
	FAC_ID UUID NOT NULL, -- FACILITY WHERE THE DATA WAS ENTERED
	ACT_UTC TIMESTAMPTZ NOT NULL, -- THE TIME THE ACT OCCURRED
	CONSTRAINT PK_AEFI_TBL PRIMARY KEY (ACT_ID),
	CONSTRAINT FK_AEFI_CRT_WHSE_ETL FOREIGN KEY (CRT_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_AEFI_UPD_WHSE_ETL FOREIGN KEY (UPD_ETL_ID) REFERENCES WHSE_ETL_TBL(ETL_ID),
	CONSTRAINT FK_AEFI_PVD_ID FOREIGN KEY (PVD_ID) REFERENCES PVD_TBL(PVD_ID),
	CONSTRAINT FK_AEFI_MMAT_ID FOREIGN KEY (MMAT_ID) REFERENCES MMAT_TBL(MMAT_ID),
	CONSTRAINT FK_AEFI_ENC_ID FOREIGN KEY (ENC_ID) REFERENCES ENC_TBL(ENC_ID),
	CONSTRAINT FK_AEFI_PAT_ID FOREIGN KEY (PAT_ID) REFERENCES PAT_TBL(PAT_ID),
	CONSTRAINT FK_AEFI_FAC_ID FOREIGN KEY (FAC_ID) REFERENCES FAC_TBL(FAC_ID)
);

-- NAME CROSSTAB VIEW
DROP MATERIALIZED VIEW IF EXISTS ENT_NAME_PIVOT_VW;
CREATE MATERIALIZED VIEW ENT_NAME_PIVOT_VW as 
select * from 
	crosstab(
	$$ 
		select name_id::text || ent_id::text, ent_id, use_cs, typ, value from name_cmp_tbl order by 1, 2; 
	$$,
	$$ 
		select 'Other' as cd_mnemonic 
		union 
		select cd_mnemonic from cd_tbl where set_mnemonic = 'NameComponentType' order by cd_mnemonic; 
	$$)
	AS ct (
	"agg_fn" text,
	"ent_id" uuid,
	"use" text,
	Delimiter text,
	Family text,
	Given text, 
	Other text,
	Prefix text, 
	Suffix text,
	Title text
);

-- ADDRESS VIEW PIVOT
DROP MATERIALIZED VIEW IF EXISTS ENT_ADDR_PIVOT_VW;
CREATE MATERIALIZED VIEW ENT_ADDR_PIVOT_VW as 
select * from 
	crosstab(
	$$ 
		select addr_id::text || ent_id::text, ent_id, use_cs, typ, value from addr_cmp_tbl order by 1, 2; 
	$$,
	$$ 
		select 'Other' as cd_mnemonic 
		union 
		select cd_mnemonic from cd_tbl where set_mnemonic = 'AddressComponentType' order by cd_mnemonic; 
	$$)
	AS ct (
	"agg_fn" text,
	"ent_id" uuid,
	"use" text,
	AdditionalLocator text,
	AddressLine text,
	BuildingNumber text,
	BuildingNumberNumeric text,
	BuildingNumberSuffix text,
	CareOf text,
	CensusTract text,
	City text,
	Country text,
	County text,
	Delimiter text,
	DeliveryAddressLine text,
	DeliveryInstallationArea text,
	DeliveryInstallationQualifier text,
	DeliveryInstallationType text,
	DeliveryMode text,
	DeliveryModeIdentifier text,
	Direction text,
	Other text,
	PostalCode text,
	PostBox text,
	Precinct text,
	State text,
	StreetAddressLine text,
	StreetName text,
	StreetNameBase text,
	StreetType text,
	UnitDesignator text,
	UnitIdentifier text

);


-- VIEW FOR COMPLETE FACILITY INFORMATION
CREATE VIEW FAC_VW AS
	SELECT FAC_TBL.*, name.other AS loc_name, mother.StreetAddressLine, mother.City, mother.County, mother.State, mother.Censustract, mother.Country, mother.PostalCode, mother.AdditionalLocator
	FROM FAC_TBL
	LEFT JOIN ent_name_pivot_vw AS name ON (name.ent_id = fac_tbl.fac_id)
	LEFT JOIN ent_addr_pivot_vw AS addr ON (mother.ent_id = fac_tbl.fac_id);

-- PERSON VIEW
CREATE OR REPLACE VIEW psn_vw AS 
	SELECT psn_tbl.*,
		name.family,
		name.given,
		addr.StreetAddressLine, 
		addr.City, 
		addr.County, 
		addr.State, 
		addr.Censustract, 
		addr.Country, 
		addr.PostalCode, 
		addr.AdditionalLocator
	FROM psn_tbl
		LEFT JOIN ent_name_pivot_vw AS name ON (psn_tbl.psn_id = name.ent_id)
		LEFT JOIN ent_addr_pivot_vw AS addr ON (psn_tbl.psn_id = addr.ent_id);
-- PATIENT VIEW
CREATE OR REPLACE VIEW pat_vw AS 
	SELECT 
		pat_tbl.pat_id,
		pat_tbl.gender_mnemonic,
		psn_vw.*, 
		mother.family as mth_family,
		mother.given as mth_given,
		mother.alt_id as mth_alt_id,
		mother.alt_id_type as mth_alt_id_type,
		nok.family as nok_family,
		nok.given as nok_given,
		nok.alt_id as nok_alt_id,
		nok.alt_id_type as nok_alt_id_type,
		pat_tbl.deceased,
		asgn.loc_name as fac_name,
		parent.loc_name as parent_fac_name,
		parent.fac_id as parent_fac_id
	FROM
		pat_tbl
		INNER JOIN psn_vw USING(psn_id)
		LEFT JOIN psn_vw AS mother ON (mother.psn_id = pat_tbl.mth_id)
		LEFT JOIN psn_vw AS nok ON (nok.psn_id = pat_tbl.nok_id)
		LEFT JOIN FAC_VW AS asgn ON (asgn.fac_id = pat_tbl.asgn_fac_id)
		LEFT JOIN FAC_VW AS parent ON (asgn.parent_id = parent.fac_id);
