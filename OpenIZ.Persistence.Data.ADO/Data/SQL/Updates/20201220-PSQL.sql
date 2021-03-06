﻿/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20201214-01" name="Update:20201214-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Add PURGED status key</summary>
 *	<remarks>Adds status key PURGED to the database</remarks>
 *	<isInstalled>select ck_patch('20201214-01')</isInstalled>
 * </feature>
 */
BEGIN TRANSACTION;

INSERT INTO CD_TBL (CD_ID, IS_SYS) VALUES ('39995C08-0A5C-4549-8BA7-D187F9B3C4FD', TRUE);
INSERT INTO CD_VRSN_TBL (CD_ID, STS_CD_ID, CLS_ID, CRT_USR_ID, CRT_UTC, MNEMONIC) VALUES ('39995C08-0A5C-4549-8BA7-D187F9B3C4FD', 'C8064CBD-FA06-4530-B430-1A52F1530C27', '54B93182-FC19-47A2-82C6-089FD70A4F45', 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8', CURRENT_TIMESTAMP, 'PURGED');
INSERT INTO CD_NAME_TBL (CD_ID, EFFT_VRSN_SEQ_ID, LANG_CS, VAL, phon_alg_id) 
	SELECT '39995C08-0A5C-4549-8BA7-D187F9B3C4FD', vrsn_seq_id, 'EN', 'Purged', '402cd339-d0e4-46ce-8fc2-12a4b0e17226'
	FROM CD_VRSN_TBL WHERE CD_ID = '39995C08-0A5C-4549-8BA7-D187F9B3C4FD';
INSERT INTO CD_SET_MEM_ASSOC_TBL  (CD_ID, SET_ID) VALUES ('39995C08-0A5C-4549-8BA7-D187F9B3C4FD', '93A48F6A-6808-4C70-83A2-D02178C2A883');
INSERT INTO CD_SET_MEM_ASSOC_TBL  (CD_ID, SET_ID) VALUES ('39995C08-0A5C-4549-8BA7-D187F9B3C4FD', 'AAE906AA-27B3-4CDB-AFF1-F08B0FD31E59');
INSERT INTO CD_SET_MEM_ASSOC_TBL  (CD_ID, SET_ID) VALUES ('39995C08-0A5C-4549-8BA7-D187F9B3C4FD', 'C7578340-A8FF-4D7D-8105-581016324E68');

ALTER TABLE ACT_VRSN_TBL DROP CONSTRAINT ck_act_vrsn_act_utc;
ALTER TABLE ACT_VRSN_TBL ADD CONSTRAINT ck_act_vrsn_act_utc CHECK (sts_cd_id = '39995C08-0A5C-4549-8BA7-D187F9B3C4FD' OR ((act_utc IS NOT NULL) OR (act_start_utc IS NOT NULL) OR (act_stop_utc IS NOT NULL)));
SELECT REG_PATCH('20201214-01');

COMMIT;
