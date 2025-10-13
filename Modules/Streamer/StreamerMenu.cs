using TMPro;
using UnityEngine;

using TownOfHost.Templates;
using static TownOfHost.GameSettingMenuStartPatch;
using static TownOfHost.Translator;
using Object = UnityEngine.Object;

namespace TownOfHost
{
    class CreateStreameMenu
    {
        public static SimpleButton OpenMenu;
        public static SimpleButton SetURL; public static TextMeshPro NowURLText; public static TextMeshPro NowURLTitle;
        public static TextBoxTMP JoinWord; public static TextMeshPro JoinWordTitle;
        public static TextBoxTMP RemoveWord; public static TextMeshPro RemoveWordTitle;
        public static void CreateMenu(MainMenuManager mainmenumanager)
        {
            var textbox = GameObject.Find("AccountManager/AccountTab/AccountWindow/SubWindows/EditName/NameText");
            if (SimpleButton.IsNullOrDestroyed(OpenMenu))
            {
                OpenMenu = MainMenuManagerPatch.CreateButton(
                    "OpenStreamMenu",
                    new Vector3(2.4036f, -2.6963f, 1f),
                    new(245, 46, 32, byte.MaxValue),
                    new(212, 113, 106, byte.MaxValue),
                    () =>
                    {
                        SetOpenMenu();
                        CredentialsPatch.TohkLogo.gameObject.SetActive(false);
                    },
                    "Stream"
                    );
                {
                    JoinWord = Object.Instantiate(textbox, mainmenumanager.screenTint.transform).GetComponent<TextBoxTMP>();
                    RemoveWord = Object.Instantiate(textbox, mainmenumanager.screenTint.transform).GetComponent<TextBoxTMP>();
                    DestroyNameMono(JoinWord.gameObject);
                    DestroyNameMono(RemoveWord.gameObject);
                    JoinWord.name = "joinword";
                    RemoveWord.name = "removeword";
                    JoinWord.gameObject.SetActive(false);
                    RemoveWord.gameObject.SetActive(false);
                    JoinWord.allowAllCharacters = true;
                    JoinWord.AllowSymbols = true;
                    JoinWord.AllowEmail = true;
                    JoinWord.AllowPaste = true;
                    JoinWord.characterLimit = 20;
                    JoinWord.SetText(Main.JoinWord.Value);
                    RemoveWord.allowAllCharacters = true;
                    RemoveWord.AllowSymbols = true;
                    RemoveWord.AllowEmail = true;
                    RemoveWord.AllowPaste = true;
                    RemoveWord.characterLimit = 20;
                    RemoveWord.SetText(Main.RemoveWord.Value);
                }
            }
            if (SimpleButton.IsNullOrDestroyed(SetURL))
            {
                SetURL = MainMenuManagerPatch.CreateButton(
                    "PasteURL",
                    new Vector3(-1.9f, 0.85f, -5),
                    new(245, 46, 32, byte.MaxValue),
                    new(212, 113, 106, byte.MaxValue),
                    () =>
                    {
                        var url = GUIUtility.systemCopyBuffer;
                        if (url.StartsWith("https://youtube") || url.StartsWith("https://www.youtube.com/"))
                        {
                            StreamerInfo.StreamURL = url;
                            NowURLText.text = StreamerInfo.StreamURL;
                        }
                        else
                        {
                            NowURLText.text = "Error!" + GetString("ClickToSetURL");
                        }
                    },
                    "Set URL",
                    transform: mainmenumanager.screenTint.transform
                    );
                SetURL.Button.gameObject.SetActive(false);
            }

            void SetOpenMenu()
            {
                mainmenumanager.screenTint.enabled = true;
                if (JoinWord is not null)
                {
                    JoinWord.gameObject.SetActive(true);
                    JoinWord.transform.localPosition = new Vector3(-1.2f, -0.25f, 0);
                    JoinWord.transform.localScale = new Vector3(0.7f, 0.7f);
                    JoinWord.allowAllCharacters = true;
                    JoinWord.AllowSymbols = true;
                    JoinWord.AllowEmail = true;
                    JoinWord.AllowPaste = true;
                    JoinWord.characterLimit = 20;
                    JoinWord.SetText(Main.JoinWord.Value);
                }
                if (RemoveWord is not null)
                {
                    RemoveWord.gameObject.SetActive(true);
                    RemoveWord.transform.localPosition = new Vector3(-1.2f, -1.65f, -5);
                    RemoveWord.transform.localScale = new Vector3(0.7f, 0.7f);
                    RemoveWord.allowAllCharacters = true;
                    RemoveWord.AllowSymbols = true;
                    RemoveWord.AllowEmail = true;
                    RemoveWord.AllowPaste = true;
                    RemoveWord.characterLimit = 20;
                    RemoveWord.SetText(Main.RemoveWord.Value);
                }
                if (NowURLText is not null)
                {
                    NowURLText.gameObject.SetActive(true);
                    NowURLText.transform.localPosition = new Vector3(1f, 1.2267f, -5);
                }
                if (NowURLTitle is not null)
                {
                    NowURLTitle?.gameObject?.SetActive(true);
                    NowURLTitle.transform.localPosition = new Vector3(0.8f, 1.8267f, 0);
                }
                if (JoinWordTitle is not null)
                {
                    JoinWordTitle?.gameObject?.SetActive(true);
                    JoinWordTitle.transform.localPosition = new Vector3(0.8f, 0.4267f, 0);
                }
                if (RemoveWordTitle is not null)
                {
                    RemoveWordTitle?.gameObject?.SetActive(true);
                    RemoveWordTitle.transform.localPosition = new Vector3(0.8f, -0.9733f, 0);
                }
                if (SetURL is not null)
                    SetURL?.Button?.gameObject?.SetActive(true);
            }

            void DestroyNameMono(GameObject @object)
            {
                if (@object.TryGetComponent<NameTextBehaviour>(out var nameTextBehaviour))
                {
                    Object.Destroy(nameTextBehaviour);
                }
            }
        }

        public static void CloseMenu()
        {
            if (JoinWord?.text is not "" and not null) Main.JoinWord.Value = JoinWord.text;
            if (RemoveWord?.text is not "" and not null) Main.RemoveWord.Value = RemoveWord.text;
            try
            {
                StreamerInfo.SetstreamKey();
                JoinWord?.gameObject?.SetActive(false);
                RemoveWord?.gameObject?.SetActive(false);
                NowURLText?.gameObject?.SetActive(false);
                SetURL?.Button?.gameObject?.SetActive(false);
                NowURLTitle?.gameObject?.SetActive(false);
                JoinWordTitle?.gameObject?.SetActive(false);
                RemoveWordTitle?.gameObject?.SetActive(false);
            }
            catch { }
        }

        public static void CreateText()
        {
            {
                NowURLText = TMPTemplate.Create("Nowurl", GetString("Unsettled"), Color.white, 2, TextAlignmentOptions.TopLeft, false);
                NowURLText.text = StreamerInfo.StreamURL is "" ? GetString("ClickToSetURL") : StreamerInfo.StreamURL;
                NowURLTitle = TMPTemplate.Create("NowURLTitle", $"<b><u>{GetString("NowURLTitle")}</b>", Color.white, 3, TextAlignmentOptions.TopLeft, false);
                JoinWordTitle = TMPTemplate.Create("JoinWordTitle", $"<u><b>{GetString("JoinWordTitle")}</b>", Color.white, 3, TextAlignmentOptions.TopLeft, false);
                RemoveWordTitle = TMPTemplate.Create("RemoveWordTitle", $"<u><b>{GetString("RemoveWordTitle")}</b>", Color.white, 3, TextAlignmentOptions.TopLeft, false);
            }
        }
    }

    public class HopeInfo
    {
        public string Comment;
        public string PlayerName;
        public string AccountName;
        public string AccountID;
        public int Id;

        public static HopeInfo TryCrateHopeInfo(string comment, string accountid, string accountname)
        {
            if (comment.Contains(Main.JoinWord.Value))
            {
                var playername = comment.RemoveDeltext(Main.JoinWord.Value);
                return new HopeInfo(comment, accountid, accountname, playername);
            }
            return null;
        }
        HopeInfo(string comment, string accountid, string accountname, string playername)
        {
            this.Comment = comment;
            this.AccountID = accountid;
            this.AccountName = accountname;
            this.PlayerName = playername;
            Id = -1;
        }

        public bool IsUpdate(HopeInfo info)
        {
            //アカウントIDが一緒かつ、AmongUsのプレイヤー名が異なる
            if (this.AccountID == info.AccountID && info.PlayerName != this.PlayerName) return true;
            else return false;
        }

        public void SetId(int id)
        {
            this.Id = id;
        }
    }
}