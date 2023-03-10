using HarmonyLib;
using Il2Cpp;
using Il2CppAddressable;
using Il2CppBattleUI.Abnormality;
using Il2CppSimpleJSON;
using Il2CppSteamworks;
using Il2CppStorySystem;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Il2CppTMPro;
using Il2CppUtilityUI;
using LimbusLocalize;
using MelonLoader;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(LimbusLocalizeMod), LimbusLocalizeMod.NAME, LimbusLocalizeMod.VERSION, LimbusLocalizeMod.AUTHOR)]
namespace LimbusLocalize
{
    public class LimbusLocalizeMod : MelonMod
    {
        public static string path;
        public static TMP_FontAsset tmpchinesefont;
        public const string NAME = "LimbusLocalizeMod";
        public const string VERSION = "0.1.3";
        public const string AUTHOR = "Bright";
        public override void OnInitializeMelon()
        {
            path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!Directory.Exists(path + "/.hide"))
            {
                Directory.CreateDirectory(path + "/.hide");
                FileAttributes MyAttributes = File.GetAttributes(path + "/.hide");
                File.SetAttributes(path + "/.hide", MyAttributes | FileAttributes.Hidden);
            }
            TranslateJSON.Setup();
            HarmonyLib.Harmony harmony = new("LimbusLocalizeMod");
            harmony.PatchAll(typeof(LimbusLocalizeMod));

            //使用AssetBundle技术载入中文字库
            tmpchinesefont = AssetBundle.LoadFromFile(path + "/tmpchinesefont").LoadAsset("assets/sourcehansanssc-heavy sdf.asset").Cast<TMP_FontAsset>();
        }
        [HarmonyPatch(typeof(AbnormalityChoiceDialogController), nameof(AbnormalityChoiceDialogController.SetDialogAfterJudgementData))]
        [HarmonyPrefix]
        public static bool SetDialogAfterJudgementData(AbnormalityChoiceDialogController __instance, CHOICE_EVENT_RESULT state)
        {
            AB_DLG_EVENT_TYPE ab_DLG_EVENT_TYPE = (state.Equals(CHOICE_EVENT_RESULT.SUCCESS) ? AB_DLG_EVENT_TYPE.SUCCESS : AB_DLG_EVENT_TYPE.FAILURE);
            string voiceId = string.Format("{0}_{1}", (ab_DLG_EVENT_TYPE == AB_DLG_EVENT_TYPE.SUCCESS) ? "choice_success_p" : "choice_fail_n", __instance._characterID);
            System.Func<TextData_PersonalityVoice, bool> Findmatch = (TextData_PersonalityVoice x) => x.ID.Contains(voiceId);
            TextData_PersonalityVoice textData_PersonalityVoice = Singleton<TextDataManager>.Instance.personalityVoiceText.GetDataList(__instance._characterID)?.DataList.Find(Findmatch);
            if (textData_PersonalityVoice != null)
            {
                __instance._dialogAfterJudgementText = textData_PersonalityVoice.GetDialog();
                VoiceGenerator.PlayBasicVoice(__instance._characterID, textData_PersonalityVoice.ID);
                return false;
            }
            __instance._dialogAfterJudgementText = Singleton<TextDataManager>.Instance.AbnormalityEventCharDlg.GetDlg(__instance._characterID, ab_DLG_EVENT_TYPE);
            return false;
        }
        [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.fontMaterial), MethodType.Setter)]
        [HarmonyPrefix]
        private static bool set_fontMaterial(TMP_Text __instance, Material value)
        {
            //防止字库变动
            value = __instance.font.material;
            bool check = __instance.gameObject.name.StartsWith("[Tmpro]SkillMinPower") || __instance.gameObject.name.StartsWith("[Tmpro]SkillMaxPower");
            //处理不正确大小
            if (!check && __instance.fontSize >= 50f)
            {
                __instance.fontSize -= __instance.fontSize / 50f * 20f;
            }
            if (__instance.m_sharedMaterial != null && __instance.m_sharedMaterial.GetInstanceID() == value.GetInstanceID())
            {
                return false;
            }
            __instance.m_sharedMaterial = value;
            __instance.m_padding = __instance.GetPaddingForMaterial();
            __instance.m_havePropertiesChanged = true;
            __instance.SetVerticesDirty();
            __instance.SetMaterialDirty();
            return false;
        }
        [HarmonyPatch(typeof(TextMeshProLanguageSetter), nameof(TextMeshProLanguageSetter.UpdateTMP))]
        [HarmonyPrefix]
        private static bool UpdateTMP(TextMeshProLanguageSetter __instance, LOCALIZE_LANGUAGE lang)
        {
            //使用中文字库
            var fontAsset = tmpchinesefont;
            __instance._text.font = fontAsset;
            __instance._text.fontMaterial = fontAsset.material;
            if (__instance._matSetter != null)
            {
                __instance._matSetter.defaultMat = fontAsset.material;
                __instance._matSetter.ResetMaterial();
                return false;
            }
            __instance.gameObject.TryGetComponent<TextMeshProMaterialSetter>(out TextMeshProMaterialSetter textMeshProMaterialSetter);
            if (textMeshProMaterialSetter != null)
            {
                textMeshProMaterialSetter.defaultMat = fontAsset.material;
                textMeshProMaterialSetter.ResetMaterial();
            }
            return false;
        }

        [HarmonyPatch(typeof(TextDataManager), nameof(TextDataManager.LoadRemote))]
        [HarmonyPrefix]
        private static bool LoadRemote(LOCALIZE_LANGUAGE lang)
        {
            //载入所有文本
            var tm = TextDataManager.Instance;
            tm._isLoadedRemote = true;
            TextDataManager.RomoteLocalizeFileList romoteLocalizeFileList = JsonUtility.FromJson<TextDataManager.RomoteLocalizeFileList>(SingletonBehavior<AddressableManager>.Instance.LoadAssetSync<TextAsset>("Assets/Resources_moved/Localize", "RemoteLocalizeFileList", null, null).Item1.ToString());
            tm._uiList.Init(romoteLocalizeFileList.UIFilePaths);
            tm._characterList.Init(romoteLocalizeFileList.CharacterFilePaths);
            tm._personalityList.Init(romoteLocalizeFileList.PersonalityFilePaths);
            tm._enemyList.Init(romoteLocalizeFileList.EnemyFilePaths);
            tm._egoList.Init(romoteLocalizeFileList.EgoFilePaths);
            tm._skillList.Init(romoteLocalizeFileList.SkillFilePaths);
            tm._passiveList.Init(romoteLocalizeFileList.PassiveFilePaths);
            tm._bufList.Init(romoteLocalizeFileList.BufFilePaths);
            tm._itemList.Init(romoteLocalizeFileList.ItemFilePaths);
            tm._keywordList.Init(romoteLocalizeFileList.keywordFilePaths);
            tm._skillTagList.Init(romoteLocalizeFileList.skillTagFilePaths);
            tm._abnormalityEventList.Init(romoteLocalizeFileList.abnormalityEventsFilePath);
            tm._attributeList.Init(romoteLocalizeFileList.attributeTextFilePath);
            tm._abnormalityCotentData.Init(romoteLocalizeFileList.abnormalityGuideContentFilePath);
            tm._keywordDictionary.Init(romoteLocalizeFileList.keywordDictionaryFilePath);
            tm._actionEvents.Init(romoteLocalizeFileList.actionEventsFilePath);
            tm._egoGiftData.Init(romoteLocalizeFileList.egoGiftFilePath);
            tm._stageChapter.Init(romoteLocalizeFileList.stageChapterPath);
            tm._stagePart.Init(romoteLocalizeFileList.stagePartPath);
            tm._stageNodeText.Init(romoteLocalizeFileList.stageNodeInfoPath);
            tm._dungeonNodeText.Init(romoteLocalizeFileList.dungeonNodeInfoPath);
            tm._storyDungeonNodeText.Init(romoteLocalizeFileList.storyDungeonNodeInfoPath);
            tm._quest.Init(romoteLocalizeFileList.Quest);
            tm._dungeonArea.Init(romoteLocalizeFileList.dungeonAreaPath);
            tm._battlePass.Init(romoteLocalizeFileList.BattlePassPath);
            tm._storyTheater.Init(romoteLocalizeFileList.StoryTheater);
            tm._announcer.Init(romoteLocalizeFileList.Announcer);
            tm._normalBattleResultHint.Init(romoteLocalizeFileList.NormalBattleHint);
            tm._abBattleResultHint.Init(romoteLocalizeFileList.AbBattleHint);
            tm._tutorialDesc.Init(romoteLocalizeFileList.TutorialDesc);
            tm._iapProductText.Init(romoteLocalizeFileList.IAPProduct);
            tm._userInfoBannerDesc.Init(romoteLocalizeFileList.UserInfoBannerDesc);
            tm._illustGetConditionText.Init(romoteLocalizeFileList.GetConditionText);
            tm._choiceEventResultDesc.Init(romoteLocalizeFileList.ChoiceEventResult);
            tm._battlePassMission.Init(romoteLocalizeFileList.BattlePassMission);
            tm._gachaTitle.Init(romoteLocalizeFileList.GachaTitle);
            tm._introduceCharacter.Init(romoteLocalizeFileList.IntroduceCharacter);
            tm._userBanner.Init(romoteLocalizeFileList.UserBanner);

            tm._abnormalityEventCharDlg.AbEventCharDlgRootInit(romoteLocalizeFileList.abnormalityCharDlgFilePath);
            tm._personalityVoiceText.PersonalityVoiceJsonDataListInit(romoteLocalizeFileList.PersonalityVoice);
            tm._announcerVoiceText.AnnouncerVoiceJsonDataListInit(romoteLocalizeFileList.AnnouncerVoice);
            tm._bgmLyricsText.BgmLyricsJsonDataListInit(romoteLocalizeFileList.BgmLyrics);
            tm._egoVoiceText.EGOVoiceJsonDataListInit(romoteLocalizeFileList.EGOVoice);

            return false;
        }
        public static bool isgameupdate;
        [HarmonyPatch(typeof(TextDataManager), nameof(TextDataManager.LoadLocal))]
        [HarmonyPrefix]
        private static bool LoadLocal(LOCALIZE_LANGUAGE lang)
        {
            var tm = TextDataManager.Instance;
            TextDataManager.LocalizeFileList localizeFileList = JsonUtility.FromJson<TextDataManager.LocalizeFileList>(Resources.Load<TextAsset>("Localize/LocalizeFileList").ToString());
            tm._loginUIList.Init(localizeFileList.LoginUIFilePaths);
            tm._fileDownloadDesc.Init(localizeFileList.FileDownloadDesc);
            tm._battleHint.Init(localizeFileList.BattleHint);
            return false;
        }
        [HarmonyPatch(typeof(StoryData), nameof(StoryData.Init))]
        [HarmonyPrefix]
        private static bool StoryDataInit(StoryData __instance)
        {
            //载入所有剧情
            ScenarioAssetDataList scenarioAssetDataList = JsonUtility.FromJson<ScenarioAssetDataList>(File.ReadAllText(LimbusLocalizeMod.path + "/Localize/CN/CN_NickName.json"));
            __instance._modelAssetMap = new Dictionary<string, ScenarioAssetData>();
            __instance._standingAssetMap = new Dictionary<string, StandingAsset>();
            __instance._standingAssetPathMap = new Dictionary<string, string>();
            foreach (ScenarioAssetData scenarioAssetData in scenarioAssetDataList.assetData)
            {
                string name = scenarioAssetData.name;
                __instance._modelAssetMap.Add(name, scenarioAssetData);
                if (!string.IsNullOrEmpty(scenarioAssetData.fileName) && !__instance._standingAssetPathMap.ContainsKey(scenarioAssetData.fileName))
                    __instance._standingAssetPathMap.Add(scenarioAssetData.fileName, "Story_StandingModel" + scenarioAssetData.fileName);
            }
            ScenarioMapAssetDataList scenarioMapAssetDataList = JsonUtility.FromJson<ScenarioMapAssetDataList>(Resources.Load<TextAsset>("Story/ScenarioMapCode").ToString());
            __instance._mapAssetMap = new Dictionary<string, ScenarioMapAssetData>();
            foreach (ScenarioMapAssetData scenarioMapAssetData in scenarioMapAssetDataList.assetData)
                __instance._mapAssetMap.Add(scenarioMapAssetData.id, scenarioMapAssetData);
            __instance._emotionMap = new Dictionary<string, EmotionAsset>();
            for (int i = 0; i < __instance._emotions.Count; i++)
                __instance._emotionMap.Add(__instance._emotions[i].prefab.Name.ToLower(), __instance._emotions[i]);
            return false;
        }
        [HarmonyPatch(typeof(StoryData), nameof(StoryData.GetScenario))]
        [HarmonyPrefix]
        private static bool GetScenario(StoryData __instance, string scenarioID, LOCALIZE_LANGUAGE lang, ref Scenario __result)
        {
            //读取剧情
            string item = File.ReadAllText(LimbusLocalizeMod.path + "/Localize/CN/CN_" + scenarioID + ".json");
            TextAsset textAsset = SingletonBehavior<AddressableManager>.Instance.LoadAssetSync<TextAsset>("Assets/Resources_moved/Story/Effect", scenarioID, null, null).Item1;
            if (textAsset == null)
            {
                textAsset = SingletonBehavior<AddressableManager>.Instance.LoadAssetSync<TextAsset>("Assets/Resources_moved/Story/Effect", "SDUMMY", null, null).Item1;
            }
            string text3 = item;
            string text4 = textAsset.ToString();
            Scenario scenario = new Scenario();
            scenario.ID = scenarioID;
            JSONArray jsonarray = JSONNode.Parse(text3)["dataList"].AsArray;
            JSONArray jsonarray2 = JSONNode.Parse(text4)["dataList"].AsArray;
            for (int i = 0; i < jsonarray.Count; i++)
            {
                int num = jsonarray[i]["id"];
                if (num >= 0)
                {
                    JSONNode jsonnode = new JSONObject();
                    if (jsonarray2[i]["id"] == num)
                    {
                        jsonnode = jsonarray2[i];
                    }
                    scenario.Scenarios.Add(new Dialog(num, jsonarray[i], jsonnode));
                }
            }
            __result = scenario;
            return false;
        }
        [HarmonyPatch(typeof(StoryData), nameof(StoryData.GetTellerTitle))]
        [HarmonyPrefix]
        private static bool GetTellerTitle(StoryData __instance, string name, LOCALIZE_LANGUAGE lang, ref string __result)
        {
            //剧情称号
            var entries = __instance._modelAssetMap._entries;
            var Entr = __instance._modelAssetMap.FindEntry(name);
            ScenarioAssetData scenarioAssetData = Entr == -1 ? null : entries?[Entr].value;
            if (scenarioAssetData != null)
                __result = scenarioAssetData.nickName;
            return false;
        }
        [HarmonyPatch(typeof(StoryData), nameof(StoryData.GetTellerName))]
        [HarmonyPrefix]
        private static bool GetTellerName(StoryData __instance, string name, LOCALIZE_LANGUAGE lang, ref string __result)
        {
            //剧情名字
            var entries = __instance._modelAssetMap._entries;
            var Entr = __instance._modelAssetMap.FindEntry(name);
            ScenarioAssetData scenarioAssetData = Entr == -1 ? null : entries?[Entr].value;
            if (scenarioAssetData != null)
                __result = scenarioAssetData.krname;
            return false;
        }
        [HarmonyPatch(typeof(LoginSceneManager), nameof(LoginSceneManager.SetLoginInfo))]
        [HarmonyPostfix]
        private static void SetLoginInfo(LoginSceneManager __instance)
        {
            string SteamID = SteamClient.SteamId.ToString();
            LoadLocal(LOCALIZE_LANGUAGE.EN);
            //在主页右下角增加一段文本，用于指示版本号和其他内容
            var fontAsset = tmpchinesefont;
            __instance.tmp_loginAccount.font = fontAsset;
            __instance.tmp_loginAccount.fontMaterial = fontAsset.material;
            __instance.tmp_loginAccount.text = "LimbusLocalizeMod v." + VERSION;
            //增加首次使用弹窗，告知使用者不用花钱买/使用可能有封号概率等
            if (UpdateChecker.UpdateCall != null)
            {
                TranslateJSON.OpenGlobalPopup("模组更新已下载,点击确认将打开下载路径并退出游戏", default, default, "确认", UpdateChecker.UpdateCall);
                return;
            }
            if (File.Exists(LimbusLocalizeMod.path + "/.hide/checkisfirstuse"))
                if (File.ReadAllText(LimbusLocalizeMod.path + "/.hide/checkisfirstuse") == SteamID + " true")
                    return;
            UserAgreementUI userAgreementUI = UnityEngine.Object.Instantiate(__instance._userAgreementUI, __instance._userAgreementUI.transform.parent);
            userAgreementUI.gameObject.SetActive(true);
            userAgreementUI.tmp_popupTitle.GetComponent<UITextDataLoader>().enabled = false;
            userAgreementUI.tmp_popupTitle.text = "首次使用提示";
            var textMeshProUGUI = userAgreementUI._userAgreementContent._agreementJP.GetComponentInChildren<TextMeshProUGUI>(true);
            System.Action<bool> _ontogglevaluechange = delegate (bool on)
            {
                if (userAgreementUI._userAgreementContent.Agreed())
                {
                    textMeshProUGUI.text = "模因封号触媒启动\r\n\r\n检测到存活迹象\r\n\r\n解开安全锁";
                    userAgreementUI._userAgreementContent.toggle_userAgreements.gameObject.SetActive(false);
                    userAgreementUI.btn_confirm.interactable = true;
                }
            };
            userAgreementUI._userAgreementContent.Init(_ontogglevaluechange);
            System.Action _onclose = delegate ()
            {
                File.WriteAllText(LimbusLocalizeMod.path + "/.hide/checkisfirstuse", SteamID + " true");
                userAgreementUI.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(userAgreementUI);
                UnityEngine.Object.Destroy(userAgreementUI.gameObject);
            };
            userAgreementUI._panel.closeEvent.AddListener(_onclose);
            System.Action _oncancel = delegate ()
            {
                SteamClient.Shutdown();
                Application.Quit();
            };
            userAgreementUI.btn_cancel._onClick.AddListener(_oncancel);
            userAgreementUI.btn_confirm.interactable = false;
            System.Action _onconfirm = userAgreementUI.OnConfirmClicked;
            userAgreementUI.btn_confirm._onClick.AddListener(_onconfirm);
            userAgreementUI._collectionOfPersonalityInfo.gameObject.SetActive(false);
            userAgreementUI._userAgreementContent._scrollRect.content = userAgreementUI._userAgreementContent._agreementJP;
            textMeshProUGUI.font = fontAsset;
            textMeshProUGUI.fontMaterial = fontAsset.material;
            textMeshProUGUI.text = "<link=\"https://github.com/Bright1192/LimbusLocalize\">点我进入Github链接</link>\n该mod完全免费\n零协会是唯一授权发布对象\n警告：使用模组会有微乎其微的封号概率(如果他们检测这个的话)\n你已经被警告过了";
            var textMeshProUGUI2 = userAgreementUI._userAgreementContent.toggle_userAgreements.GetComponentInChildren<TextMeshProUGUI>(true);
            textMeshProUGUI2.GetComponent<UITextDataLoader>().enabled = false;
            textMeshProUGUI2.font = fontAsset;
            textMeshProUGUI2.fontMaterial = fontAsset.material;
            textMeshProUGUI2.text = "点击进行身份认证";
            userAgreementUI._userAgreementContent.transform.localPosition = new Vector3(510f, 77f);
            userAgreementUI._userAgreementContent.toggle_userAgreements.gameObject.SetActive(true);
            userAgreementUI._userAgreementContent._agreementJP.gameObject.SetActive(true);
            userAgreementUI._userAgreementContent.img_titleBg.gameObject.SetActive(false);
            float preferredWidth = userAgreementUI._userAgreementContent.tmp_title.preferredWidth;
            Vector2 sizeDelta = userAgreementUI._userAgreementContent.img_titleBg.rectTransform.sizeDelta;
            sizeDelta.x = preferredWidth + 60f;
            userAgreementUI._userAgreementContent.img_titleBg.rectTransform.sizeDelta = sizeDelta;
            userAgreementUI._userAgreementContent._userAgreementsScrollbar.value = 1f;
            userAgreementUI._userAgreementContent._userAgreementsScrollbar.size = 0.3f;
        }
        //Il2CppMainUI.NoticeUIPopup
        //Il2Cpp.MainLobbyRightUpperUIButton
        //待开始功能-贡献
        public static void OPEN()
        {
            string x = "{\"list\":[{\"formatKey\":\"SubTitle\",\"formatValue\":\"Github\"},{\"formatKey\":\"HyperLink\",\"formatValue\":\"https://github.com/Bright1192/LimbusLocalize\"}]}";
        }
#if DEBUG
        [HarmonyPatch(typeof(AddressablePopup), nameof(AddressablePopup.OnDownloadingYes))]
        [HarmonyPrefix]
        private static bool OnDownloadingYes(AddressablePopup __instance)
        {
            TranslateJSON.OpenGlobalPopup("检测到游戏进行了热更新,是否机翻更新后变动/新增的文本?\n如果是将根据你是否挂梯子使用谷歌/有道翻译\n如果否将根据你是否挂梯子将更新后文本保留为韩文/英文", default, default, default, delegate ()
            {
                TranslateJSON.DoTranslate = true;
                TranslateJSON.TranslateCall = delegate ()
                {
                    __instance._updateMovieScreen.SetActive(false);
                    SingletonBehavior<AddressableManager>.Instance.InitLoad();
                    __instance.Close();
                };
                TranslateJSON.StartTranslate();
            }, delegate ()
            {
                TranslateJSON.DoTranslate = false;
                TranslateJSON.TranslateCall = delegate ()
                {
                    __instance._updateMovieScreen.SetActive(false);
                    SingletonBehavior<AddressableManager>.Instance.InitLoad();
                    __instance.Close();
                };
                TranslateJSON.StartTranslate();
            });
            return false;
        }
#endif
    }
}
