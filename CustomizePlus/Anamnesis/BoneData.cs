// © Customize+.
// Licensed under the MIT license.

namespace Anamnesis.Posing
{
	using CustomizePlus;
	using Dalamud.Utility;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;

	public static class BoneData
	{
		//TODO move the csv data to an external (compressed?) file
		private static readonly string[] BoneRawTable = new string[]
		{
			//Codename, Display Name, Bone Family, Hroth Feat, Viera Feat, IVCS, Editable, Mirrored Bone (if any)
			"n_root,Root,Root,TRUE,TRUE,TRUE,FALSE,FALSE,",
			"j_kao,Head,Spine,TRUE,TRUE,TRUE,FALSE,TRUE,",
			"j_kubi,Neck,Spine,TRUE,TRUE,TRUE,FALSE,TRUE,",
			"n_hara,Abdomen,Spine,TRUE,TRUE,TRUE,FALSE,FALSE,",
			"j_sebo_c,Spine C,Spine,TRUE,TRUE,TRUE,FALSE,TRUE,",
			"j_sebo_b,Spine B,Spine,TRUE,TRUE,TRUE,FALSE,TRUE,",
			"j_sebo_a,Spine A,Spine,TRUE,TRUE,TRUE,FALSE,TRUE,",
			"j_kosi,Waist,Spine,TRUE,TRUE,TRUE,FALSE,TRUE,",
			"j_kami_a,Hair A,Hair,TRUE,TRUE,TRUE,FALSE,TRUE,",
			"j_kami_b,Hair B,Hair,TRUE,TRUE,TRUE,FALSE,TRUE,",
			"j_kami_f_l,Hair Front Left,Hair,TRUE,TRUE,TRUE,FALSE,TRUE,j_kami_f_r",
			"j_kami_f_r,Hair Front Right,Hair,TRUE,TRUE,TRUE,FALSE,TRUE,j_kami_f_l",
			"j_f_mayu_l,Eyebrow Left,Face,TRUE,TRUE,TRUE,FALSE,TRUE,j_f_mayu_r",
			"j_f_mayu_r,Eyebrow Right,Face,TRUE,TRUE,TRUE,FALSE,TRUE,j_f_mayu_l",
			"j_f_miken_l,Brow Left,Face,TRUE,TRUE,TRUE,FALSE,TRUE,j_f_miken_r",
			"j_f_miken_r,Brow Right,Face,TRUE,TRUE,TRUE,FALSE,TRUE,j_f_miken_l",
			"j_f_memoto,Bridge,Face,TRUE,TRUE,TRUE,FALSE,TRUE,",
			"j_f_umab_l,Eyelid Upper Left,Face,TRUE,TRUE,TRUE,FALSE,TRUE,j_f_umab_r",
			"j_f_umab_r,Eyelid Upper Right,Face,TRUE,TRUE,TRUE,FALSE,TRUE,j_f_umab_l",
			"j_f_dmab_l,Eyelid Lower Left,Face,TRUE,FALSE,TRUE,FALSE,TRUE,j_f_dmab_r",
			"j_f_dmab_r,Eyelid Lower Right,Face,TRUE,FALSE,TRUE,FALSE,TRUE,j_f_dmab_l",
			"j_f_eye_l,Eye Left,Face,TRUE,FALSE,TRUE,FALSE,TRUE,j_f_eye_r",
			"j_f_eye_r,Eye Right,Face,TRUE,FALSE,TRUE,FALSE,TRUE,j_f_eye_l",
			"j_f_hoho_l,Cheek Left,Face,TRUE,FALSE,TRUE,FALSE,TRUE,j_f_hoho_r",
			"j_f_hoho_r,Cheek Right,Face,TRUE,FALSE,TRUE,FALSE,TRUE,j_f_hoho_l",
			"j_f_hige_l,Hrothgar Whiskers Left,Face,FALSE,TRUE,FALSE,FALSE,TRUE,j_f_hige_r",
			"j_f_hige_r,Hrothgar Whiskers Right,Face,FALSE,TRUE,FALSE,FALSE,TRUE,j_f_hige_l",
			"j_f_hana,Nose,Face,TRUE,FALSE,TRUE,FALSE,TRUE,",
			"j_f_lip_l,Lips Left,Face,TRUE,FALSE,TRUE,FALSE,TRUE,j_f_lip_r",
			"j_f_lip_r,Lips Right,Face,TRUE,FALSE,TRUE,FALSE,TRUE,j_f_lip_l",
			"j_f_ulip_a,Lip Upper A,Face,TRUE,FALSE,TRUE,FALSE,TRUE,",
			"j_f_ulip_b,Lip Upper B,Face,TRUE,FALSE,TRUE,FALSE,TRUE,",
			"j_f_dlip_a,Lip Lower A,Face,TRUE,FALSE,TRUE,FALSE,TRUE,",
			"j_f_dlip_b,Lip Lower B,Face,TRUE,FALSE,TRUE,FALSE,TRUE,",
			"n_f_lip_l,Hrothgar Lips Left,Face,FALSE,TRUE,FALSE,FALSE,TRUE,n_f_lip_r",
			"n_f_lip_r,Hrothgar Lips Right,Face,FALSE,TRUE,FALSE,FALSE,TRUE,n_f_lip_l",
			"n_f_ulip_l,Hrothgar Lip Upper Left,Face,FALSE,TRUE,FALSE,FALSE,TRUE,n_f_ulip_r",
			"n_f_ulip_r,Hrothgar Lip Upper Right,Face,FALSE,TRUE,FALSE,FALSE,TRUE,n_f_ulip_l",
			"j_f_dlip,Hrothgar Lip Lower,Face,FALSE,TRUE,FALSE,FALSE,TRUE,",
			"j_ago,Jaw,Face,TRUE,FALSE,TRUE,FALSE,TRUE,",
			"j_f_uago,Hrothgar Jaw Upper,Face,FALSE,TRUE,FALSE,FALSE,TRUE,",
			"j_f_ulip,Hrothgar Jaw Lower,Face,FALSE,TRUE,FALSE,FALSE,TRUE,",
			"j_mimi_l,Ear Left,Ears,TRUE,TRUE,FALSE,FALSE,TRUE,j_mimi_r",
			"j_mimi_r,Ear Right,Ears,TRUE,TRUE,FALSE,FALSE,TRUE,j_mimi_l",
			"j_zera_a_l,Viera Ear 01 A Left,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zera_a_r",
			"j_zera_a_r,Viera Ear 01 A Right,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zera_a_l",
			"j_zera_b_l,Viera Ear 01 B Left,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zera_b_r",
			"j_zera_b_r,Viera Ear 01 B Right,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zera_b_l",
			"j_zerb_a_l,Viera Ear 02 A Left,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zerb_a_r",
			"j_zerb_a_r,Viera Ear 02 A Right,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zerb_a_l",
			"j_zerb_b_l,Viera Ear 02 B Left,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zerb_b_r",
			"j_zerb_b_r,Viera Ear 02 B Right,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zerb_b_l",
			"j_zerc_a_l,Viera Ear 03 A Left,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zerc_a_r",
			"j_zerc_a_r,Viera Ear 03 A Right,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zerc_a_l",
			"j_zerc_b_l,Viera Ear 03 B Left,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zerc_b_r",
			"j_zerc_b_r,Viera Ear 03 B Right,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zerc_b_l",
			"j_zerd_a_l,Viera Ear 04 A Left,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zerd_a_r",
			"j_zerd_a_r,Viera Ear 04 A Right,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zerd_a_l",
			"j_zerd_b_l,Viera Ear 04 B Left,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zerd_b_r",
			"j_zerd_b_r,Viera Ear 04 B Right,Ears,FALSE,FALSE,TRUE,FALSE,TRUE,j_zerd_b_l",
			"j_sako_l,Clavicle Left,Chest,TRUE,TRUE,TRUE,FALSE,TRUE,j_sako_r",
			"j_sako_r,Clavicle Right,Chest,TRUE,TRUE,TRUE,FALSE,TRUE,j_sako_l",
			"j_mune_l,Breast Left,Chest,TRUE,TRUE,TRUE,FALSE,TRUE,j_mune_r",
			"j_mune_r,Breast Right,Chest,TRUE,TRUE,TRUE,FALSE,TRUE,j_mune_l",
			"iv_c_mune_l,Breast B Left,Chest,FALSE,FALSE,FALSE,TRUE,TRUE,iv_c_mune_r",
			"iv_c_mune_r,Breast B Right,Chest,FALSE,FALSE,FALSE,TRUE,TRUE,iv_c_mune_l",
			"n_hkata_l,Shoulder Left,Arms,TRUE,TRUE,TRUE,FALSE,TRUE,n_hkata_r",
			"n_hkata_r,Shoulder Right,Arms,TRUE,TRUE,TRUE,FALSE,TRUE,n_hkata_l",
			"j_ude_a_l,Arm Left,Arms,TRUE,TRUE,TRUE,FALSE,TRUE,j_ude_a_r",
			"j_ude_a_r,Arm Right,Arms,TRUE,TRUE,TRUE,FALSE,TRUE,j_ude_a_l",
			"iv_nitoukin_l,Bicep Left,Arms,TRUE,FALSE,FALSE,TRUE,TRUE,iv_nitoukin_r",
			"iv_nitoukin_r,Bicep Right,Arms,TRUE,FALSE,FALSE,TRUE,TRUE,iv_nitoukin_l",
			"n_hhiji_l,Elbow Left,Arms,TRUE,TRUE,TRUE,FALSE,TRUE,n_hhiji_r",
			"n_hhiji_r,Elbow Right,Arms,TRUE,TRUE,TRUE,FALSE,TRUE,n_hhiji_l",
			"j_ude_b_l,Forearm Left,Arms,TRUE,TRUE,TRUE,FALSE,TRUE,j_ude_b_r",
			"j_ude_b_r,Forearm Right,Arms,TRUE,TRUE,TRUE,FALSE,TRUE,j_ude_b_l",
			"n_hte_l,Wrist Left,Arms,TRUE,TRUE,TRUE,FALSE,TRUE,n_hte_r",
			"n_hte_r,Wrist Right,Arms,TRUE,TRUE,TRUE,FALSE,TRUE,n_hte_l",
			"j_te_l,Hand Left,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_te_r",
			"j_te_r,Hand Right,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_te_l",
			"j_oya_a_l,Thumb A Left,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_oya_a_r",
			"j_oya_a_r,Thumb A Right,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_oya_a_l",
			"j_oya_b_l,Thumb B Left,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_oya_b_r",
			"j_oya_b_r,Thumb B Right,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_oya_b_l",
			"j_hito_a_l,Index A Left,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_hito_a_r",
			"j_hito_a_r,Index A Right,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_hito_a_l",
			"j_hito_b_l,Index B Left,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_hito_b_r",
			"j_hito_b_r,Index B Right,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_hito_b_l",
			"iv_hito_c_l,Index C Left,Hands,FALSE,FALSE,FALSE,TRUE,TRUE,iv_hito_c_r",
			"iv_hito_c_r,Index C Right,Hands,FALSE,FALSE,FALSE,TRUE,TRUE,iv_hito_c_l",
			"j_naka_a_l,Middle A Left,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_naka_a_r",
			"j_naka_a_r,Middle A Right,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_naka_a_l",
			"j_naka_b_l,Middle B Left,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_naka_b_r",
			"j_naka_b_r,Middle B Right,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_naka_b_l",
			"iv_naka_c_l,Middle C Left,Hands,FALSE,FALSE,FALSE,TRUE,TRUE,iv_naka_c_r",
			"iv_naka_c_r,Middle C Right,Hands,FALSE,FALSE,FALSE,TRUE,TRUE,iv_naka_c_l",
			"j_kusu_a_l,Ring A Left,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_kusu_a_r",
			"j_kusu_a_r,Ring A Right,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_kusu_a_l",
			"j_kusu_b_l,Ring B Left,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_kusu_b_r",
			"j_kusu_b_r,Ring B Right,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_kusu_b_l",
			"iv_kusu_c_l,Ring C Left,Hands,FALSE,FALSE,FALSE,TRUE,TRUE,iv_kusu_c_r",
			"iv_kusu_c_r,Ring C Right,Hands,FALSE,FALSE,FALSE,TRUE,TRUE,iv_kusu_c_l",
			"j_ko_a_l,Pinky A Left,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_ko_a_r",
			"j_ko_a_r,Pinky A Right,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_ko_a_l",
			"j_ko_b_l,Pinky B Left,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_ko_b_r",
			"j_ko_b_r,Pinky B Right,Hands,TRUE,TRUE,TRUE,FALSE,TRUE,j_ko_b_l",
			"iv_ko_c_l,Pinky C Left,Hands,FALSE,FALSE,FALSE,TRUE,TRUE,iv_ko_c_r",
			"iv_ko_c_r,Pinky C Right,Hands,FALSE,FALSE,FALSE,TRUE,TRUE,iv_ko_c_l",
			"n_sippo_a,Tail A,Tail,TRUE,TRUE,TRUE,FALSE,TRUE,",
			"n_sippo_b,Tail B,Tail,TRUE,TRUE,TRUE,FALSE,TRUE,",
			"n_sippo_c,Tail C,Tail,TRUE,TRUE,TRUE,FALSE,TRUE,",
			"n_sippo_d,Tail D,Tail,TRUE,TRUE,TRUE,FALSE,TRUE,",
			"n_sippo_e,Tail E,Tail,TRUE,TRUE,TRUE,FALSE,TRUE,",
			"iv_shiri_l,Buttock Left,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,iv_shiri_r",
			"iv_shiri_r,Buttock Right,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,iv_shiri_l",
			"iv_kougan_l,Scrotum Left,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,iv_kougan_r",
			"iv_kougan_r,Scrotum Right,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,iv_kougan_l",
			"iv_ochinko_a,Penis A,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,",
			"iv_ochinko_b,Penis B,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,",
			"iv_ochinko_c,Penis C,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,",
			"iv_ochinko_d,Penis D,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,",
			"iv_ochinko_e,Penis E,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,",
			"iv_ochinko_f,Penis F,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,",
			"iv_omanko,Vagina,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,",
			"iv_kuritto,Clitoris,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,",
			"iv_inshin_l,Labia Left,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,iv_inshin_r",
			"iv_inshin_r,Labia Right,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,iv_inshin_l",
			"iv_koumon,Anus,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,",
			"iv_koumon_l,Anus B Right,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,iv_koumon_r",
			"iv_koumon_r,Anus B Left,Groin,FALSE,FALSE,FALSE,TRUE,TRUE,iv_koumon_l",
			"j_asi_a_l,Leg Left,Legs,TRUE,TRUE,TRUE,FALSE,TRUE,j_asi_a_r",
			"j_asi_a_r,Leg Right,Legs,TRUE,TRUE,TRUE,FALSE,TRUE,j_asi_a_l",
			"j_asi_b_l,Knee Left,Legs,TRUE,TRUE,TRUE,FALSE,TRUE,j_asi_b_r",
			"j_asi_b_r,Knee Right,Legs,TRUE,TRUE,TRUE,FALSE,TRUE,j_asi_b_l",
			"j_asi_c_l,Calf Left,Legs,TRUE,TRUE,TRUE,FALSE,TRUE,j_asi_c_r",
			"j_asi_c_r,Calf Right,Legs,TRUE,TRUE,TRUE,FALSE,TRUE,j_asi_c_l",
			"j_asi_d_l,Foot Left,Feet,TRUE,TRUE,TRUE,FALSE,TRUE,j_asi_d_r",
			"j_asi_d_r,Foot Right,Feet,TRUE,TRUE,TRUE,FALSE,TRUE,j_asi_d_l",
			"j_asi_e_l,Toes Left,Feet,TRUE,TRUE,TRUE,FALSE,TRUE,j_asi_e_r",
			"j_asi_e_r,Toes Right,Feet,TRUE,TRUE,TRUE,FALSE,TRUE,j_asi_e_l",
			"j_asi_oya_a_l,Big Toe A Left,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_oya_a_r",
			"j_asi_oya_a_r,Big Toe A Right,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_oya_a_l",
			"j_asi_oya_b_l,Big Toe B Left,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_oya_b_r",
			"j_asi_oya_b_r,Big Toe B Right,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_oya_b_l",
			"j_asi_hito_a_l,Index Toe A Left,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_hito_a_r",
			"j_asi_hito_a_r,Index Toe A Right,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_hito_a_l",
			"j_asi_hito_b_l,Index Toe B Left,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_hito_b_r",
			"j_asi_hito_b_r,Index Toe B Right,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_hito_b_l",
			"j_asi_naka_a_l,Middle Toe A Left,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_naka_a_r",
			"j_asi_naka_a_r,Middle Toe A Right,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_naka_a_l",
			"j_asi_naka_b_l,Middle Toe B Left,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_naka_b_r",
			"j_asi_naka_b_r,Middle Toe B Right,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_naka_b_l",
			"j_asi_kusu_a_l,Fore Toe A Left,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_kusu_a_r",
			"j_asi_kusu_a_r,Fore Toe A Right,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_kusu_a_l",
			"j_asi_kusu_b_l,Fore Toe B Left,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_kusu_b_r",
			"j_asi_kusu_b_r,Fore Toe B Right,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_kusu_b_l",
			"j_asi_ko_a_l,Pinky Toe A Left,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_ko_a_r",
			"j_asi_ko_a_r,Pinky Toe A Right,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_ko_a_l",
			"j_asi_ko_b_l,Pinky Toe B Left,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_ko_b_r",
			"j_asi_ko_b_r,Pinky Toe B Right,Feet,FALSE,FALSE,FALSE,TRUE,TRUE,j_asi_ko_b_l",
			"n_ear_b_l,Earring B Left,Earrings,TRUE,TRUE,TRUE,FALSE,TRUE,n_ear_b_r",
			"n_ear_b_r,Earring B Right,Earrings,TRUE,TRUE,TRUE,FALSE,TRUE,n_ear_b_l",
			"n_ear_a_l,Earring A Left,Earrings,TRUE,TRUE,TRUE,FALSE,TRUE,n_ear_a_r",
			"n_ear_a_r,Earring A Right,Earrings,TRUE,TRUE,TRUE,FALSE,TRUE,n_ear_a_l",
			"j_ex_top_a_l,Cape A Left,Cape,TRUE,TRUE,TRUE,FALSE,TRUE,j_ex_top_a_r",
			"j_ex_top_a_r,Cape A Right,Cape,TRUE,TRUE,TRUE,FALSE,TRUE,j_ex_top_a_l",
			"j_ex_top_b_l,Cape B Left,Cape,TRUE,TRUE,TRUE,FALSE,TRUE,j_ex_top_b_r",
			"j_ex_top_b_r,Cape B Right,Cape,TRUE,TRUE,TRUE,FALSE,TRUE,j_ex_top_b_l",
			"n_kataarmor_l,Pauldron Left,Sleeves,TRUE,TRUE,TRUE,FALSE,FALSE,n_kataarmor_r",
			"n_kataarmor_r,Pauldron Right,Sleeves,TRUE,TRUE,TRUE,FALSE,FALSE,n_kataarmor_l",
			"n_hijisoubi_l,Couter Left,Sleeves,TRUE,TRUE,TRUE,FALSE,FALSE,n_hijisoubi_r",
			"n_hijisoubi_r,Couter Right,Sleeves,TRUE,TRUE,TRUE,FALSE,FALSE,n_hijisoubi_l",
			"j_sk_b_a_l,Cloth Back A Left,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_b_a_r",
			"j_sk_b_a_r,Cloth Back A Right,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_b_a_l",
			"j_sk_b_b_l,Cloth Back B Left,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_b_b_r",
			"j_sk_b_b_r,Cloth Back B Right,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_b_b_l",
			"j_sk_b_c_l,Cloth Back C Left,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_b_c_r",
			"j_sk_b_c_r,Cloth Back C Right,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_b_c_l",
			"j_sk_f_a_l,Cloth Front A Left,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_f_a_r",
			"j_sk_f_a_r,Cloth Front A Right,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_f_a_l",
			"j_sk_f_b_l,Cloth Front B Left,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_f_b_r",
			"j_sk_f_b_r,Cloth Front B Right,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_f_b_l",
			"j_sk_f_c_l,Cloth Front C Left,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_f_c_r",
			"j_sk_f_c_r,Cloth Front C Right,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_f_c_l",
			"j_sk_s_a_l,Cloth Side A Left,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_s_a_r",
			"j_sk_s_a_r,Cloth Side A Right,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_s_a_l",
			"j_sk_s_b_l,Cloth Side B Left,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_s_b_r",
			"j_sk_s_b_r,Cloth Side B Right,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_s_b_l",
			"j_sk_s_c_l,Cloth Side C Left,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_s_c_r",
			"j_sk_s_c_r,Cloth Side C Right,Skirt,TRUE,TRUE,TRUE,FALSE,FALSE,j_sk_s_c_l",
			"n_hizasoubi_l,Poleyn Left,Pants,TRUE,TRUE,TRUE,FALSE,FALSE,n_hizasoubi_r",
			"n_hizasoubi_r,Poleyn Right,Pants,TRUE,TRUE,TRUE,FALSE,FALSE,n_hizasoubi_l",
			"n_throw,Throw,Equipment,TRUE,TRUE,TRUE,FALSE,FALSE,",
			"j_buki_sebo_l,Scabbard Left,Equipment,TRUE,TRUE,TRUE,FALSE,FALSE,j_buki_sebo_r",
			"j_buki_sebo_r,Scabbard Right,Equipment,TRUE,TRUE,TRUE,FALSE,FALSE,j_buki_sebo_l",
			"j_buki2_kosi_l,Holster Left,Equipment,TRUE,TRUE,TRUE,FALSE,FALSE,j_buki2_kosi_r",
			"j_buki2_kosi_r,Holster Right,Equipment,TRUE,TRUE,TRUE,FALSE,FALSE,j_buki2_kosi_l",
			"j_buki_kosi_l,Sheath Left,Equipment,TRUE,TRUE,TRUE,FALSE,FALSE,j_buki_kosi_r",
			"j_buki_kosi_r,Sheath Right,Equipment,TRUE,TRUE,TRUE,FALSE,FALSE,j_buki_kosi_l",
			"n_buki_tate_l,Shield Left,Equipment,TRUE,TRUE,TRUE,FALSE,FALSE,n_buki_tate_r",
			"n_buki_tate_r,Shield Right,Equipment,TRUE,TRUE,TRUE,FALSE,FALSE,n_buki_tate_l",
			"n_buki_l,Weapon Left,Equipment,TRUE,TRUE,TRUE,FALSE,FALSE,n_buki_r",
			"n_buki_r,Weapon Right,Equipment,TRUE,TRUE,TRUE,FALSE,FALSE,n_buki_l",
		};

		public enum BoneFamily
		{
			Root, Spine, Hair, Face, Ears, Chest, Arms, Hands, Tail, Groin, Legs, Feet,
			Earrings, Cape, Sleeves, Skirt, Pants, Equipment
		}

		public static BoneFamily[] DisplayableFamilies = new BoneFamily[]
		{
			BoneFamily.Spine,
			BoneFamily.Hair,
			BoneFamily.Face,
			BoneFamily.Ears,
			BoneFamily.Chest,
			BoneFamily.Arms,
			BoneFamily.Hands,
			BoneFamily.Tail,
			BoneFamily.Groin,
			BoneFamily.Legs,
			BoneFamily.Feet,
			BoneFamily.Earrings,
			BoneFamily.Cape
		};

		public struct BoneDatum
		{
			public string Codename;
			public string DisplayName;
			public BoneFamily Family;
			public bool StandardFeature;
			public bool HrothgarFeature;
			public bool VieraFeature;
			public bool IVCSFeature;
			public bool Editable;
			public string? MirroredCodename;

			public BoneDatum(string[] fields)
			{
				int i = 0;

				Codename = fields[i++];
				DisplayName = fields[i++];

				Family = ParseFamilyName(fields[i++]);

				StandardFeature = bool.Parse(fields[i++]);
				HrothgarFeature = bool.Parse(fields[i++]);
				VieraFeature = bool.Parse(fields[i++]);
				IVCSFeature = bool.Parse(fields[i++]);

				Editable = bool.Parse(fields[i++]);
				MirroredCodename = fields[i].IsNullOrEmpty() ? null : fields[i];
			}
		}

		private static readonly Dictionary<string, BoneDatum> BoneTable;

		private static readonly Dictionary<string, string> BoneLookupByDispName = new Dictionary<string, string>();

		static BoneData()
		{
			BoneTable = new();

			int rowIndex = 0;
			foreach(string entry in BoneRawTable)
			{
				try
				{
					string[] cells = entry.Split(',');
					string codename = cells[0];
					string dispName = cells[1];

					BoneTable[codename] = new BoneDatum(cells);
					BoneLookupByDispName[dispName] = codename;
				}
				catch
				{
					throw new InvalidCastException($"Couldn't parse raw bone table @ row {rowIndex}");
				}

				++rowIndex;
			}
		}

		public static string? GetBoneDisplayName(string codename)
		{
			return BoneTable.TryGetValue(codename, out BoneDatum row) ? row.DisplayName : null;
		}

		public static string? GetBoneCodename(string boneDisplayName)
		{
			return BoneLookupByDispName.TryGetValue(boneDisplayName, out string? name) ? name : null;
		}

		public static List<string> GetBoneCodenames()
		{
			return BoneTable.Keys.ToList();
		}

		public static List<string> GetBoneDisplayNames()
		{
			return BoneLookupByDispName.Keys.ToList();
		}

		public static IEnumerable<string> GetStandardBoneCodenames()
		{
			return BoneTable.Where(x => x.Value.StandardFeature).Select(x => x.Key);
		}
		public static IEnumerable<string> GetFilteredBoneCodenames(bool forHrothgar, bool forViera, bool forIVCS, bool includeRoot = false)
		{
			return BoneTable
				.Where(x => FitsFlags(x.Value, forHrothgar, forViera, forIVCS, includeRoot))
				.Select(x => x.Key);
		}
		public static IEnumerable<string> GetFilteredBoneCodenames(BodyScale bs, bool includeRoot = false)
		{
			return GetFilteredBoneCodenames(bs.InclHroth, bs.InclViera, bs.InclIVCS, includeRoot);
		}

		public static Dictionary<string, Tuple<string, BoneFamily, string?>> GetEditableBoneInfo(bool forHrothgar, bool forViera, bool includeIVCS)
		{
			return BoneTable
				.Where(x => FitsFlags(x.Value, forHrothgar, forViera, includeIVCS, false))
				.ToDictionary(x => x.Key, x => new Tuple<string, BoneFamily, string?>(x.Value.DisplayName, x.Value.Family, x.Value.MirroredCodename));
		}

		private static bool FitsFlags(BoneDatum datum, bool forHrothgar, bool forViera, bool includeIVCS, bool includeRoot)
		{
			return (datum.Editable || (datum.Codename == "n_root" && includeRoot))
				&&
				(datum.StandardFeature
				|| (datum.HrothgarFeature && forHrothgar && !forViera)
				|| (datum.VieraFeature && forViera && !forHrothgar)
				|| (datum.IVCSFeature && includeIVCS));
		}

		public static BoneFamily GetBoneFamily(string codename)
		{
			return BoneTable.TryGetValue(codename, out BoneDatum row) ? row.Family : (BoneFamily)(-1);
		}

		public static string? GetBoneMirror(string codename)
		{
			return BoneTable.TryGetValue(codename, out BoneDatum row) ? row.MirroredCodename : null;
		}

		public static bool IsEditableBone(string codename)
		{
			return BoneTable.TryGetValue(codename, out BoneDatum row) ? row.Editable : false;
		}

		public static bool IsHrothgarBone(string codename)
		{
			return BoneTable.TryGetValue(codename, out BoneDatum row)
				? row.HrothgarFeature && ! row.StandardFeature && !row.VieraFeature : false;
		}
		public static bool IsVieraBone(string codename)
		{
			return BoneTable.TryGetValue(codename, out BoneDatum row)
				? row.VieraFeature && ! row.StandardFeature && !row.HrothgarFeature : false;
		}
		public static bool IsIVCSBone(string codename)
		{
			return BoneTable.TryGetValue(codename, out BoneDatum row) ? row.IVCSFeature : false;
		}

		private static BoneFamily ParseFamilyName(string n)
		{
			string simplified = n.Split(' ').FirstOrDefault()?.ToLower() ?? String.Empty;

			BoneFamily fam = simplified switch
			{
				"root" => BoneFamily.Root,
				"spine" => BoneFamily.Spine,
				"face" => BoneFamily.Face,
				"chest" => BoneFamily.Chest,
				"arms" => BoneFamily.Arms,
				"hands" => BoneFamily.Hands,
				"tail" => BoneFamily.Tail,
				"groin" => BoneFamily.Groin,
				"legs" => BoneFamily.Legs,
				"feet" => BoneFamily.Feet,
				"ears" => BoneFamily.Ears,
				"hair" => BoneFamily.Hair,
				"earrings" => BoneFamily.Earrings,
				"cape" => BoneFamily.Cape,
				"sleeves" => BoneFamily.Sleeves,
				"skirt" => BoneFamily.Skirt,
				"pants" => BoneFamily.Pants,
				"equipment" => BoneFamily.Equipment,
				_ => (BoneFamily)(-1)
			};

			return fam;
		}
	}
}