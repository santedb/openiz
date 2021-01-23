﻿/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20181113-01" name="Update:20181113-01" applyRange="1.0.0.0-1.10.0.0"  invariantName="npgsql">
 *	<summary>Update: Optimize check constraints</summary>
 *	<remarks>Adds a more space efficient unique check on the act relationship, entity and participation tables</remarks>
 *	<isInstalled>select ck_patch('20181113-01')</isInstalled>
 * </feature>
 */

 BEGIN TRANSACTION;

drop index ent_id_ent_val_uq_idx ;
CREATE UNIQUE INDEX ent_id_ent_val_uq_idx ON ent_id_tbl (efft_vrsn_seq_id, aut_id, id_val) where (obslt_vrsn_seq_id is null);
--#!
create unique index act_ptcpt_unq_enf_sha1 on act_ptcpt_tbl(digest(act_id::text || ent_id::text || rol_cd_id::text, 'sha1')) where (obslt_vrsn_seq_id is null);
drop index act_ptcpt_unq_enf;
--#!
create unique index act_rel_unq_enf_sha1 on act_rel_tbl(digest(src_Act_id::text || trg_act_id::text || rel_typ_cd_id::text, 'sha1')) where (obslt_vrsn_seq_id is null);
drop index act_rel_unq_enf;
--#!
create unique index ent_rel_unq_enf_sha1 on ent_rel_tbl(digest(src_ent_id::text || trg_ent_id::text || rel_typ_cd_id::text, 'sha1')) where (obslt_vrsn_seq_id is null);
drop index ent_rel_unq_enf;
--#!
drop index if exists ent_tbl_cls_cd_id_idx;
create index if not exists ent_cls_cd_idx on ent_tbl(cls_cd_id);

SELECT REG_PATCH('20181113-01');

COMMIT;