"""Builds Assets/Editor/Geometry/zanatomy_map.json: which Z-Anatomy objects make up each explorer entity id.
Z-Anatomy (CC BY-SA 4.0, derived from BodyParts3D CC BY-SA 2.1 JP) is exported by export.py to ZAnatomyData/.
Rules are (id, [regex on the Z object name], exclude-regex). Z names carry a .l/.r side suffix."""
import json, re, sys, os
here = os.path.dirname(os.path.abspath(__file__))
root = os.path.abspath(os.path.join(here, '..', '..'))
idx = json.load(open(os.path.join(root, 'ZAnatomyData', 'zana_index.json')))
BAD = r'bursa|\bfascia\b|sheath|septum of|retinaculum|aponeurosis|ligament|node'
names = [i['name'] for i in idx if i['coll'][:2] in ('1:', '3:', '4:', '5:', '6:', '7:', '8:') and not re.search(r'\.[gjt]$', i['name'])]
R = []
def rule(i, pats, ex=None): R.append((i, pats if isinstance(pats, list) else [pats], ex))

# ---- skeleton
rule('SYS_SK_CLAVICLE', r'^Clavicle\.')
rule('SYS_SK_PELVIS', r'^Hip bone\.')
rule('SYS_SK_FEMUR', r'^Femur\.'); rule('SYS_SK_TIBIA', r'^Tibia\.'); rule('SYS_SK_HUMERUS', r'^Humerus\.')
rule('SYS_SK_SCAPULA', r'^Scapula\.'); rule('SYS_SK_PATELLA', r'^Patella\.'); rule('SYS_SK_FIBULA', r'^Fibula\.')
rule('SYS_SK_RADIUS', r'^Radius\.'); rule('SYS_SK_ULNA', r'^Ulna\.')
rule('SYS_SK_STERNUM', r'^(Manubrium of sternum|Body of sternum|Xiphoid process)$')
rule('SYS_SK_MANDIBLE', r'^Mandible'); rule('SYS_SK_FRONTAL', r'^Frontal bone'); rule('SYS_SK_PARIETAL', r'^Parietal bone')
rule('SYS_SK_OCCIPITAL', r'^Occipital bone'); rule('SYS_SK_TEMPORAL', r'^Temporal bone'); rule('SYS_SK_SPHENOID', r'^Sphenoid bone')
rule('SYS_SK_ZYGOMATIC', r'^Zygomatic bone'); rule('SYS_SK_MAXILLA', r'^Maxilla'); rule('SYS_SK_NASAL', r'^Nasal bone')
rule('SYS_SK_TEETH', r'^(Upper|Lower) '); rule('SYS_SK_HYOID', r'^Hyoid bone')
rule('SYS_SK_ATLAS', r'^Atlas'); rule('SYS_SK_AXIS', r'^Axis')
rule('SYS_SK_VERT_CERVICAL', r'^Vertebra C[3-7]$'); rule('SYS_SK_VERT_THORACIC', r'^Vertebra T\d+$'); rule('SYS_SK_VERT_LUMBAR', r'^Vertebra L\d$')
rule('SYS_SK_SACRUM', r'^Sacrum$'); rule('SYS_SK_COCCYX', r'^Coccyx$')
rule('SYS_SK_DISC', r'^Intervertebral disc')
rule('SYS_SK_RIB_TRUE', r'^(First|Second|Third|Fourth|Fifth|Sixth|Seventh) rib\.')
rule('SYS_SK_RIB_FALSE', r'^(Eighth|Ninth|Tenth) rib\.')
rule('SYS_SK_RIB_FLOATING', r'^(Eleventh|Twelfth) rib\.')
rule('SYS_SK_COSTAL_CARTILAGE', r'^Costal cartilage of')
rule('SYS_SK_CARPALS', r'^(Scaphoid|Lunate|Triquetr\w*|Pisiform|Trapezium|Trapezoid|Capitate|Hamate) bone')
rule('SYS_SK_METACARPALS', r'metacarpal bone'); rule('SYS_SK_PHALANGES_HAND', r'phalanx of .* of hand')
rule('SYS_SK_TALUS', r'^Talus'); rule('SYS_SK_CALCANEUS', r'^Calcaneus\.')
rule('SYS_SK_TARSALS', r'^(Navicular|Cuboid|(Medial|Intermediate|Lateral) cuneiform) bone')
rule('SYS_SK_METATARSALS', r'metatarsal bone'); rule('SYS_SK_PHALANGES_FOOT', r'phalanx of .* of foot')
rule('SYS_SK_ACL', r'^Anterior cruciate ligament'); rule('SYS_SK_PCL', r'^Posterior cruciate ligament')
rule('SYS_SK_MCL', r'^(Superficial part of )?[Tt]ibial collateral ligament|^Deep part of tibial collateral')
rule('SYS_SK_LCL', r'^Fibular collateral ligament')
rule('SYS_SK_MENISCUS', r'^(Lateral|Medial) meniscus\.')
rule('SYS_SK_NUCHAL_LIGAMENT', r'^Nuchal ligament')
rule('SYS_SK_PUBIC_SYMPHYSIS', r'^(Pubic symphysis|Interpubic disc)')
rule('SYS_SK_PLANTAR_FASCIA', r'^Plantar aponeurosis')
rule('SYS_RESP_LARYNX', r'^(Thyroid cartilage|Cricoid cartilage|Arytenoid cartilage|Corniculate cartilage)')

# ---- muscles
def m(i, p, ex=None): rule(i, p, ex or BAD)
m('SYS_MUSC_BICEPS', r'biceps brachii'); m('SYS_MUSC_PECTORALIS', r'pectoralis major')
m('SYS_MUSC_DELTOID', r'part of deltoid'); m('SYS_MUSC_RECTUS_ABDOMINIS', r'^Rectus abdominis')
m('SYS_MUSC_GASTROCNEMIUS', r'head of gastrocnemius'); m('SYS_MUSC_TRAPEZIUS', r'part of trapezius')
m('SYS_MUSC_LATISSIMUS', r'^Latissimus dorsi'); m('SYS_MUSC_TRICEPS', r'head of triceps brachii')
m('SYS_MUSC_OBLIQUE', r'^External abdominal oblique'); m('SYS_MUSC_STERNOCLEIDOMASTOID', r'^Sternocleidomastoid')
m('SYS_MUSC_TIBIALIS', r'^Tibialis anterior muscle'); m('SYS_MUSC_SOLEUS', r'^Soleus')
m('SYS_MUSC_ERECTOR_SPINAE', r'^(Iliocostalis|Longissimus|Spinalis) ')
m('SYS_MUSC_SERRATUS', r'^Serratus anterior'); m('SYS_MUSC_TEMPORALIS', r'^Temporalis muscle')
m('SYS_MUSC_MASSETER', r'masseter'); m('SYS_MUSC_FRONTALIS', r'^Frontalis')
m('SYS_MUSC_ORBICULARIS_OCULI', r'orbicularis oculi'); m('SYS_MUSC_ORBICULARIS_ORIS', r'^Orbicularis oris')
m('SYS_MUSC_ZYGOMATICUS', r'^Zygomaticus major'); m('SYS_MUSC_BUCCINATOR', r'^Bucinator')
m('SYS_MUSC_PECTORALIS_MINOR', r'^Pectoralis minor'); m('SYS_MUSC_RHOMBOIDS', r'^Rhomboid ')
m('SYS_MUSC_LEVATOR_SCAPULAE', r'^Levator scapulae'); m('SYS_MUSC_SUPRASPINATUS', r'^Supraspinatus')
m('SYS_MUSC_INFRASPINATUS', r'^Infraspinatus'); m('SYS_MUSC_TERES_MINOR', r'^Teres minor'); m('SYS_MUSC_TERES_MAJOR', r'^Teres major')
m('SYS_MUSC_SUBSCAPULARIS', r'^Subscapularis'); m('SYS_MUSC_BRACHIALIS', r'^Brachialis'); m('SYS_MUSC_CORACOBRACHIALIS', r'^Coracobrachialis')
m('SYS_MUSC_BRACHIORADIALIS', r'^Brachioradialis'); m('SYS_MUSC_PRONATOR_TERES', r'pronator teres')
m('SYS_MUSC_FLEXOR_CARPI_RADIALIS', r'^Flexor carpi radialis'); m('SYS_MUSC_PALMARIS_LONGUS', r'^Palmaris longus')
m('SYS_MUSC_FLEXOR_DIGITORUM_SUPERFICIALIS', r'flexor digitorum superficialis'); m('SYS_MUSC_FLEXOR_CARPI_ULNARIS', r'flexor carpi ulnaris')
m('SYS_MUSC_EXTENSOR_CARPI_RADIALIS', r'^Extensor carpi radialis'); m('SYS_MUSC_EXTENSOR_DIGITORUM', r'^Extensor digitorum$')
m('SYS_MUSC_EXTENSOR_CARPI_ULNARIS', r'extensor carpi ulnaris')
m('SYS_MUSC_THENAR', r'^(Abductor pollicis brevis|Opponens pollicis|(Superficial|Deep) head of flexor pollicis brevis|Oblique head of adductor pollicis|Transverse head of adductor pollicis)')
m('SYS_MUSC_HYPOTHENAR', r'^(Abductor digiti minimi of hand|Flexor digiti minimi of hand|Opponens digiti minimi muscle of hand)')
m('SYS_MUSC_INTERNAL_OBLIQUE', r'^Internal abdominal oblique'); m('SYS_MUSC_TRANSVERSUS_ABDOMINIS', r'^Transversus abdominis')
m('SYS_MUSC_QUADRATUS_LUMBORUM', r'^Quadratus lumborum'); m('SYS_MUSC_PSOAS', r'^Psoas major'); m('SYS_MUSC_ILIACUS', r'^Iliacus')
m('SYS_MUSC_SCALENES', r'^Scalenus'); m('SYS_MUSC_DIGASTRIC', r'digastric muscle')
m('SYS_MUSC_CORRUGATOR_SUPERCILII', r'^Corrugator'); m('SYS_MUSC_PROCERUS', r'^Procerus'); m('SYS_MUSC_NASALIS', r'^Nasalis')
m('SYS_MUSC_LEVATOR_LABII_SUPERIORIS', r'^Levator labii superioris'); m('SYS_MUSC_DEPRESSOR_ANGULI_ORIS', r'^Depressor anguli oris')
m('SYS_MUSC_MENTALIS', r'^Mentalis'); m('SYS_MUSC_GLUTEUS_MAXIMUS', r'^Gluteus maximus'); m('SYS_MUSC_GLUTEUS_MEDIUS', r'^Gluteus medius')
m('SYS_MUSC_PIRIFORMIS', r'^Piriformis muscle'); m('SYS_MUSC_TFL', r'^Tensor fasciae latae')
rule('SYS_MUSC_ILIOTIBIAL_TRACT', r'^Iliotibial tract')
m('SYS_MUSC_RECTUS_FEMORIS', r'^Rectus femoris'); m('SYS_MUSC_VASTUS_LATERALIS', r'^Vastus lateralis'); m('SYS_MUSC_VASTUS_MEDIALIS', r'^Vastus medialis')
m('SYS_MUSC_VASTUS_INTERMEDIUS', r'^Vastus intermedius'); m('SYS_MUSC_SARTORIUS', r'^Sartorius'); m('SYS_MUSC_GRACILIS', r'^Gracilis')
m('SYS_MUSC_ADDUCTOR_LONGUS', r'^Adductor longus'); m('SYS_MUSC_ADDUCTOR_MAGNUS', r'^Adductor magnus')
m('SYS_MUSC_BICEPS_FEMORIS', r'biceps femoris'); m('SYS_MUSC_SEMITENDINOSUS', r'^Semitendinosus'); m('SYS_MUSC_SEMIMEMBRANOSUS', r'^Semimembranosus muscle')
rule('SYS_MUSC_ACHILLES', r'^Calcaneal tendon')
m('SYS_MUSC_EXTENSOR_DIGITORUM_LONGUS', r'^Extensor digitorum longus'); m('SYS_MUSC_PERONEUS_LONGUS', r'^Fibularis longus')
m('SYS_MUSC_TIBIALIS_POSTERIOR', r'^Tibialis posterior'); m('SYS_MUSC_FLEXOR_DIGITORUM_LONGUS', r'^Flexor digitorum longus')
m('SYS_MUSC_EXTRAOCULAR', r'^(Superior|Inferior|Lateral|Medial) (rectus|oblique) muscle')
rule('SYS_RESP_DIAPHRAGM', r'^Diaphragm$', r'fascia')

# ---- organs
rule('SYS_CV_HEART', r'^(Left|Right) (ventricle|atrium)$'); rule('SYS_CV_HEART_LV', r'^Left ventricle$'); rule('SYS_CV_HEART_LA', r'^Left atrium$')
rule('SYS_CV_HEART_RA', r'^Right atrium$'); rule('SYS_CV_HEART_RV', r'^Right ventricle$')
rule('SYS_RESP_LUNG_L', r'lobe of left lung'); rule('SYS_RESP_LUNG_R', r'lobe of right lung')
rule('SYS_RESP_TRACHEA', r'^Trachea$'); rule('SYS_RESP_BRONCHI', r'^(Left|Right) main bronchus$')
rule('SYS_DIG_ESOPHAGUS', r'^Oesophagus'); rule('SYS_DIG_STOMACH', r'^Stomach$'); rule('SYS_DIG_LIVER', r'^Liver$')
rule('SYS_DIG_GALLBLADDER', r'^Gallbladder$'); rule('SYS_DIG_PANCREAS', r'^Pancreas$')
rule('SYS_REN_KIDNEY_L', r'^Kidney\.l$'); rule('SYS_REN_KIDNEY_R', r'^Kidney\.r$')
rule('SYS_REN_URETER', r'^Ureter'); rule('SYS_REN_BLADDER', r'^Urinary bladder')
rule('SYS_ENDO_THYROID', r'^Thyroid gland$'); rule('SYS_ENDO_ADRENAL', r'^Suprarenal gland'); rule('SYS_ENDO_PITUITARY', r'hypophysis$')
rule('SYS_ENDO_PINEAL', r'^Pineal gland')
rule('SYS_LYMPH_SPLEEN', r'^Spleen$'); rule('SYS_LYMPH_THYMUS', r'lobe of thymus'); rule('SYS_LYMPH_TONSILS', r'^Palatine tonsil')
rule('SYS_DIG_DUODENUM', r'^Duodenum$'); rule('SYS_DIG_JEJUNUM', r'^Jejunum$'); rule('SYS_DIG_APPENDIX', r'^Vermiform appendix')
rule('SYS_DIG_COLON_ASC', r'^Ascending colon'); rule('SYS_DIG_COLON_TRANS', r'^Transverse colon'); rule('SYS_DIG_COLON_DESC', r'^Descending colon')
rule('SYS_DIG_SIGMOID', r'^Sigmoid colon'); rule('SYS_DIG_TONGUE', r'^Tongue$')
rule('SYS_DIG_PAROTID', r'^Parotid gland'); rule('SYS_DIG_SUBMANDIBULAR', r'^Submandibular gland')
rule('SYS_DIG_PHARYNX', r'^(Nasopharynx|Oropharynx|Laryngopharynx)$'); rule('SYS_DIG_PALATE', r'^(Soft palate|Uvula of palate)$')
rule('SYS_DIG_BILE_DUCTS', r'^Bile duct$'); rule('SYS_DIG_PANCREATIC_DUCT', r'^Pancreatic duct$')
rule('SYS_RESP_EPIGLOTTIS', r'^Epiglottis$'); rule('SYS_REN_PELVIS', r'^Renal pelvis')
rule('SYS_REP_M_TESTIS', r'^Testis'); rule('SYS_REP_M_EPIDIDYMIS', r'^Epididymis'); rule('SYS_REP_M_VAS_DEFERENS', r'^Ductus deferens')
rule('SYS_REP_M_SEMINAL_VESICLE', r'^Seminal gland'); rule('SYS_REP_M_PROSTATE', r'^Prostate$'); rule('SYS_REP_M_URETHRA', r'^Urethra$')
rule('SYS_REP_M_PENIS_CAVERNOSA', r'^Corpus cavernosum of penis'); rule('SYS_REP_M_PENIS_SPONGIOSUM', r'^(Corpus spongiosum of penis|Glans penis)')
rule('SYS_SENS_EYE', r'^(Sclera|Cornea|Iris)\.'); rule('SYS_SENS_LENS', r'^Lens\.'); rule('SYS_SENS_RETINA', r'^Retina')
rule('SYS_SENS_COCHLEA', r'^Cochlea\.'); rule('SYS_SENS_EARDRUM', r'^Tympanic membrane')
rule('SYS_SENS_MALLEUS', r'^Malleus'); rule('SYS_SENS_INCUS', r'^Incus'); rule('SYS_SENS_STAPES', r'^Stapes')
rule('SYS_SENS_SEMICIRCULAR', r'^Vestibule\.')

# ---- brain
rule('SYS_NERV_CEREBELLUM', r'^(Culmen|Declive|Folium of vermis|Nodule of vermis|Pyramis of vermis|Tuber of vermis|Uvula of vermis|Lingula of cerebellum|Flocculus|Tonsil of cerebellum|.*lobule|Central lobule|Quadrangular|Biventral|Cerebellar hemisphere|Vermis)')
rule('SYS_NERV_FRONTAL_LOBE', r'(frontal gyrus|Precentral gyrus|Orbital gyri|Gyrus rectus|Paracentral)')
rule('SYS_NERV_PARIETAL_LOBE', r'(Postcentral gyrus|Supramarginal|Angular gyrus|parietal lobule|Precuneus)')
rule('SYS_NERV_TEMPORAL_LOBE', r'(temporal gyrus|Temporal pole|Transverse temporal|Fusiform|Parahippocampal|Temporal plane)')
rule('SYS_NERV_OCCIPITAL_LOBE', r'(occipital gyri|Cuneus|Lingual gyrus|Occipital pole)')
rule('SYS_NERV_MIDBRAIN', r'^Midbrain$'); rule('SYS_NERV_PONS', r'^Pons$'); rule('SYS_NERV_MEDULLA', r'^Medulla oblongata$')
rule('SYS_NERV_CORPUS_CALLOSUM', r'^Corpus callosum'); rule('SYS_NERV_THALAMUS', r'^Thalamus'); rule('SYS_NERV_HYPOTHALAMUS', r'^Hypothalamus')
rule('SYS_NERV_BASAL_GANGLIA', r'^(Caudate nucleus|Putamen|Globus pallidus)'); rule('SYS_NERV_HIPPOCAMPUS', r'^Hippocampus')
rule('SYS_NERV_AMYGDALA', r'^Amygdaloid body'); rule('SYS_NERV_VENTRICLES', r'ventricle')
rule('SYS_NERV_SPINALCORD', r'^(White matter of spinal cord|Anterior horn of spinal cord|Posterior horn of spinal cord)')
rule('SYS_NERV_CAUDA_EQUINA', r'^Cauda equina')

rule('SYS_CV_VALVE_AORTIC', r'(coronary leaflet|Non-coronary leaflet)'); rule('SYS_CV_VALVE_MITRAL', r'leaflet of left atrioventricular valve')
rule('SYS_CV_VALVE_TRICUSPID', r'leaflet of right atrioventricular valve'); rule('SYS_CV_VALVE_PULMONARY', r'semilunar leaflet of pulmonary valve')
rule('SYS_CV_PAPILLARY', r'papillary muscle')
rule('SYS_RESP_SINUS_FRONTAL', r'^Sinus of frontal bone'); rule('SYS_RESP_SINUS_SPHENOID', r'^Sinus of sphenoid bone')
rule('SYS_RESP_SINUS_ETHMOID', r'cells of ethmoid bone'); rule('SYS_RESP_TURBINATES', r'nasal concha bone'); rule('SYS_RESP_NASAL_CAVITY', r'^Mucosa of nasal cavity')
rule('SYS_LYMPH_NODES', r'nodes?(\.[lr])?$')
rule('SYS_SK_ARTICULAR_CARTILAGE', r'labrum')

# ---- vessels and nerves: built from the atlas' centre lines (tubes), not its bevelled meshes
lines_all = json.load(open(os.path.join(root, 'ZAnatomyData', 'zana_lines.json')))
# The atlas has no patellar ligament object, so lay one between the lower pole of the patella and the tibial tuberosity.
if not any(o['name'].startswith('Patellar ligament') for o in lines_all):
    for side, sx, sfx in (('l', 1, '.l'), ('r', -1, '.r')):
        pts = [(0.0871, 0.4260, -0.0145), (0.0862, 0.4120, -0.0152), (0.0837, 0.3994, -0.0100)]
        lines_all.append({'name': 'Patellar ligament' + sfx, 'coll': '4: Muscular system', 'bevel': 0.001,
                          'lines': [{'p': [c for q in pts for c in (q[0] * sx, q[1], q[2])], 'r': [6.0, 6.5, 7.0]}]})
    json.dump(lines_all, open(os.path.join(root, 'ZAnatomyData', 'zana_lines.json'), 'w'))
line_names = [o['name'] for o in lines_all if o['coll'][:2] in ('5:', '7:') or o['name'].startswith('Patellar ligament')]
L = []
def line(i, pat, ex=None): L.append((i, pat, ex))
for i, p in [
 ('AORTIC_ARCH', r'^Aortic arch'), ('AORTA_ASC', r'^Ascending aorta'), ('AORTA_DESC', r'^Thoracic aorta'), ('AORTA_ABD', r'^Abdominal aorta'),
 ('BRACHIOCEPHALIC', r'^Brachiocephalic trunk'), ('CAROTID', r'common carotid artery'), ('CAROTID_EXT', r'^External carotid artery'),
 ('CAROTID_INT', r'^Internal carotid artery'), ('JUGULAR', r'^Internal jugular vein'), ('JUGULAR_EXT', r'^External jugular vein'),
 ('SUBCLAVIAN', r'subclavian artery'), ('SUBCLAVIAN_VEIN', r'subclavian vein'), ('AXILLARY', r'^Axillary artery'), ('AXILLARY_VEIN', r'^Axillary vein'),
 ('BRACHIAL', r'^Brachial artery'), ('BRACHIAL_VEIN', r'^Brachial veins'), ('RADIAL', r'^Radial artery'), ('ULNAR', r'^Ulnar artery'),
 ('ILIAC', r'^Common iliac artery'), ('FEMORAL', r'^Femoral artery'), ('POPLITEAL', r'^Popliteal artery'), ('ANT_TIBIAL', r'^Anterior tibial artery'),
 ('POST_TIBIAL', r'^Posterior tibial artery'), ('ILIAC_VEIN', r'^Common iliac vein'), ('FEMORAL_VEIN', r'^Femoral vein'),
 ('POPLITEAL_VEIN', r'^Popliteal vein'), ('SAPHENOUS', r'^Great saphenous vein'), ('PULMONARY_ARTERY', r'^(Pulmonary trunk|Left pulmonary artery|Right pulmonary artery|Bifurcation of pulmonary trunk)'),
 ('PULMONARY_VEIN', r'(inferior|superior) pulmonary vein'), ('CELIAC', r'^Coeliac trunk'), ('SMA', r'^Superior mesenteric artery'),
 ('PORTAL_VEIN', r'^Hepatic portal vein'), ('RENAL_ARTERY', r'renal artery'), ('RENAL_VEIN', r'^(Left|Right) renal vein'),
 ('CORONARY_LAD', r'^(Anterior interventricular artery|Septal branches of anterior interventricular)'), ('CORONARY_CX', r'^Circumflex artery of heart'),
 ('CORONARY_RCA', r'^(Right coronary artery|Right inferolateral)'), ('CARDIAC_VEIN', r'^(Great cardiac vein|Coronary sinus|Middle cardiac vein)'),
 ('INTERNAL_THORACIC', r'^Internal thoracic artery'), ('INTERCOSTAL_ARTERY', r'^(Posterior intercostal arteries|First posterior|Second posterior|Supreme intercostal)'),
 ('HEPATIC_ARTERY', r'^(Common hepatic artery|Proper hepatic artery)'), ('GASTRIC_ARTERY', r'^Left gastric artery'), ('SPLENIC_ARTERY', r'^Splenic artery'),
 ('MESENTERIC_ARTERIES', r'^(Ileocolic|Middle colic artery|Right colic artery|Sigmoid arteries|Ileal branch|Colic branch)'),
 ('IMA', r'^Inferior mesenteric artery'), ('ILIAC_EXT', r'^External iliac artery'), ('ILIAC_INT', r'^Internal iliac artery'),
 ('PROFUNDA_FEMORIS', r'^Deep femoral artery'), ('PERONEAL', r'^Fibular artery'), ('DORSALIS_PEDIS', r'^Dorsalis pedis artery'),
 ('PLANTAR_ARTERY', r'^(Medial plantar artery|Lateral plantar artery|Plantar arch|Deep plantar artery)'), ('PROFUNDA_BRACHII', r'^Deep brachial artery'),
 ('PALMAR_ARCH', r'^(Deep palmar arch|Superficial palmar arch)'), ('DIGITAL_ARTERY', r'^(Proper palmar digital arteries|Common palmar digital arteries)'),
 ('SVC', r'^Superior vena cava'), ('IVC', r'^Inferior vena cava'), ('BRACHIOCEPHALIC_VEIN', r'brachiocephalic vein'),
 ('DURAL_SINUS', r'(sagittal|transverse|sigmoid|straight|petrosal|occipital|intercavernous) sinus'), ('AZYGOS', r'^(Azygos vein|Hemi-azygos|Accessory hemi)'),
 ('HEPATIC_VEIN', r'^Hepatic veins'), ('SPLENIC_VEIN', r'^Splenic vein'), ('SMV', r'^Superior mesenteric vein'), ('IMV', r'^Inferior mesenteric vein'),
 ('ILIAC_VEIN_EXT', r'^External iliac vein'), ('TIBIAL_VEIN', r'^(Anterior|Posterior) tibial veins'), ('SAPHENOUS_SMALL', r'^Small saphenous vein'),
 ('DORSAL_VENOUS_ARCH', r'^Dorsal venous arch of foot'), ('CEPHALIC', r'^Cephalic vein'), ('BASILIC', r'^Basilic vein'), ('MEDIAN_CUBITAL', r'^Median cubital vein'),
 ('FACIAL_ARTERY', r'^Facial artery'), ('TEMPORAL_ARTERY', r'^Superficial temporal artery'), ('VERTEBRAL', r'^Vertebral artery'), ('BASILAR', r'^Basilar artery'),
 ('CIRCLE_OF_WILLIS', r'communicating artery'), ('ACA', r'^Anterior cerebral artery'), ('MCA', r'^Middle cerebral artery'), ('PCA', r'^Posterior cerebral artery'),
]: line('SYS_CV_' + i, p)
for i, p in [
 ('SCIATIC', r'^Sciatic nerve'), ('BRACHIAL_PLEXUS', r'(brachial plexus|^Roots of brachial)'), ('VAGUS', r'^Vagus nerve'), ('RADIAL', r'^Radial nerve'),
 ('MEDIAN', r'^Median nerve'), ('ULNAR', r'^Ulnar nerve'), ('FEMORAL', r'^Femoral nerve'), ('TIBIAL', r'^Tibial nerve'), ('FIBULAR_COMMON', r'^Common fibular nerve'),
 ('PUDENDAL', r'^Pudendal nerve'), ('OPTIC', r'^Optic nerve'), ('TRIGEMINAL', r'^(Trigeminal nerve|Maxillary nerve|Ophthalmic nerve|Lingual nerve|Inferior alveolar nerve|Mental nerve|Buccal nerve)'),
 ('FACIAL', r'^Facial nerve'), ('INTERCOSTAL', r'^Intercostal nerves'), ('MUSCULOCUTANEOUS', r'^Musculocutaneous nerve'), ('AXILLARY', r'^Axillary nerve'),
 ('RADIAL_DEEP', r'^(Deep branch of radial nerve|Posterior interosseous nerve)'), ('DIGITAL_HAND', r'palmar digital branches'), ('SAPHENOUS', r'^Saphenous nerve'),
 ('OBTURATOR', r'^Obturator nerve'), ('FEMORAL_CUTANEOUS', r'^Lateral femoral cutaneous'), ('FIBULAR_DEEP', r'^Deep fibular nerve'),
 ('FIBULAR_SUPERFICIAL', r'^Superficial fibular nerve'), ('SURAL', r'^Sural nerve'), ('PLANTAR', r'^(Medial|Lateral) plantar nerve'), ('OLFACTORY', r'^Olfactory nerve'),
 ('OCULOMOTOR', r'^Oculomotor nerve'), ('TROCHLEAR', r'^Trochlear nerve'), ('ABDUCENS', r'^Abducens nerve'), ('VESTIBULOCOCHLEAR', r'^(Vestibulocochlear|Vestibular nerve|Cochlear nerve)'),
 ('GLOSSOPHARYNGEAL', r'^Glossopharyngeal nerve'), ('ACCESSORY', r'^Accessory nerve'), ('HYPOGLOSSAL', r'^Hypoglossal nerve'),
 ('SYMPATHETIC_TRUNK', r'^(Sympathetic trunk|Sympathetic nerves)'), ('CAUDA_EQUINA', r'^Cauda equina'),
]: line('SYS_NERV_' + i, p)
line('SYS_MUSC_PATELLAR_TENDON', r'^Patellar ligament')

out = []
seen_names = {}
for i, pats, ex in R:
    hits = []
    for n in names:
        if any(re.search(p[:-1] + r'(\.[lr])?$' if p.endswith('$') and not p.endswith(r'\.l$') and not p.endswith(r'\.r$') else p, n) for p in pats) and not (ex and re.search(ex, n)):
            hits.append(n)
    if i.startswith(('SYS_CV_', 'SYS_NERV_')):
        hits = [n for n in hits if n not in set(line_names)]   # curve objects are built from their centre lines
    if not hits:
        continue
    out.append({'id': i, 'names': hits, 'lines': []})
byid = {o['id']: o for o in out}
for i, pat, ex in L:
    hit = [n for n in line_names if re.search(pat, n) and not (ex and re.search(ex, n))]
    if not hit:
        print('NO LINES', i); continue
    byid.setdefault(i, {'id': i, 'names': [], 'lines': []})['lines'] = hit
    if byid[i] not in out: out.append(byid[i])
json.dump({'entries': out}, open(os.path.join(root, 'Assets/Editor/Geometry/zanatomy_map.json'), 'w'), indent=0)
print(len(out), 'entities mapped;', sum(len(o['names']) + len(o['lines']) for o in out), 'objects')
dict_ids = [e['entityID'] for e in json.load(open(os.path.join(root, 'Assets/StreamingAssets/Data/anatomy_dictionary.json')))]
have = {o['id'] for o in out}
print('unmapped (kept as sculpts):', len([d for d in dict_ids if d not in have]))
