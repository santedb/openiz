INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('35b13152-e43c-4bcb-8649-a9e83bee33a2','9de2a846-ddf2-4ebc-902e-84508c5089ea', '48b2ffb3-07db-47ba-ad73-fc8fb8502471', 'Person==[Citizen]==>Country');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('35b13152-e43c-4bcb-8649-a9e83bee33a2','bacd9c6f-3fa9-481e-9636-37457962804d', '48b2ffb3-07db-47ba-ad73-fc8fb8502471', 'Patient==[Citizen]==>Country');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('f3ef7e48-d8b7-4030-b431-aff7e0e1cb76','bacd9c6f-3fa9-481e-9636-37457962804d', 'ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c', 'Patient==[Birthplace]==>ServiceDeliveryLocation');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('f3ef7e48-d8b7-4030-b431-aff7e0e1cb76','bacd9c6f-3fa9-481e-9636-37457962804d', '21ab7873-8ef3-4d78-9c19-4582b3c40631', 'Patient==[Birthplace]==>Place');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('f3ef7e48-d8b7-4030-b431-aff7e0e1cb76','bacd9c6f-3fa9-481e-9636-37457962804d', '48b2ffb3-07db-47ba-ad73-fc8fb8502471', 'Patient==[Birthplace]==>Country');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('f3ef7e48-d8b7-4030-b431-aff7e0e1cb76','bacd9c6f-3fa9-481e-9636-37457962804d', 'd9489d56-ddac-4596-b5c6-8f41d73d8dc5', 'Patient==[Birthplace]==>County');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('f3ef7e48-d8b7-4030-b431-aff7e0e1cb76','bacd9c6f-3fa9-481e-9636-37457962804d', '05b85461-578b-4988-bca6-e3e94be9db76', 'Patient==[Birthplace]==>City');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('f3ef7e48-d8b7-4030-b431-aff7e0e1cb76','bacd9c6f-3fa9-481e-9636-37457962804d', '8cf4b0b0-84e5-4122-85fe-6afa8240c218', 'Patient==[Birthplace]==>State');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('b1d2148d-bb35-4337-8fe6-021f5a3ac8a3','bacd9c6f-3fa9-481e-9636-37457962804d', '9de2a846-ddf2-4ebc-902e-84508c5089ea', 'Patient==[Birthplace]==>State');
update ent_id_tbl set ent_id = '2092c150-ad34-48ae-9a8b-ecfe2cec4f74'  where id_val = 'TZ' and aut_id = 'ff6e7402-8545-48e1-8a70-ebf06f3ee4b8';

CREATE TABLE ent_rel_part_cont_tbl PARTITION OF ent_rel_tbl FOR VALUES IN ('B1D2148D-BB35-4337-8FE6-021F5A3AC8A3','25985F42-476A-4455-A977-4E97A554D710');
ALTER TABLE ent_rel_part_cont_tbl ADD CONSTRAINT pk_ent_rel_part_cont_tbl PRIMARY KEY (ent_rel_id); 
ALTER TABLE ent_rel_part_cont_tbl ADD CONSTRAINT fk_ent_rel_part_cont_trg_ent_id FOREIGN KEY (trg_ent_id) REFERENCES ent_tbl (ent_id); 
CREATE UNIQUE INDEX ent_rel_part_cont_unq_idx ON ent_rel_part_cont_tbl (digest((src_ent_id::text || trg_ent_id::text) || rel_typ_cd_id::text, 'sha1'::text)) WHERE obslt_vrsn_seq_id IS NULL;
CREATE TRIGGER ent_rel_part_cont_tbl_vrfy BEFORE INSERT OR UPDATE ON ent_rel_part_cont_tbl FOR EACH ROW EXECUTE PROCEDURE trg_vrfy_ent_rel_tbl();
