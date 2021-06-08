using UnityEngine;
using UnityEngine.UI;
#if(UNITY_2018_3_OR_NEWER)
using UnityEngine.Android;
#endif
using agora_gaming_rtc;

public class RTCManager : MonoBehaviour
{
    public static RTCManager instance;
    //public Button joinChannel;
    public Button leaveChannel;
    public Button muteButton;

    private IRtcEngine mRtcEngine = null;

    [SerializeField]
    private string AppID = "app_id";
    public GameObject groupRTCUI, peopleList, prefabHolder;
    public uint selfuserid;
    private Color color,defcolor;
    void Awake()
    {
        //QualitySettings.vSyncCount = 0;
        //Application.targetFrameRate = 30;
        muteButton.enabled = false;
    }
    public void StartFunction()
    {
        mRtcEngine = IRtcEngine.GetEngine(AppID);
        mRtcEngine.EnableAudio();
        JoinChannel();
        mRtcEngine.OnJoinChannelSuccess += (string channelName, uint uid, int elapsed) =>
        {
            string joinSuccessMessage = string.Format("joinChannel callback uid: {0}, channel: {1})", uid, channelName);
            Debug.Log(joinSuccessMessage);
            selfuserid = uid;
            //mShownMessage.GetComponent<Text>().text = (joinSuccessMessage);
            muteButton.enabled = true;
            groupRTCUI.SetActive(true);
            peopleList.SetActive(true);
            if (RTMManager.instance.pendingsrequest.Count > 0)
            {
                foreach (string peerid in RTMManager.instance.pendingsrequest)
                {
                    RTMManager.instance.SendPeerMessage(peerid);
                }
            }
        };

        mRtcEngine.OnLeaveChannel += (RtcStats stats) =>
        {
            string leaveChannelMessage = string.Format("onLeaveChannel callback duration {0}", stats.duration);
            Debug.Log(leaveChannelMessage);
            //mShownMessage.GetComponent<Text>().text = (leaveChannelMessage);
            muteButton.enabled = false;
            // reset the mute button state
            if (isMuted)
            {
                MuteButtonTapped();
            }
            RTMManager.instance.Finish();
        };

        mRtcEngine.OnUserJoined += (uint uid, int elapsed) =>
        {
            string userJoinedMessage = string.Format("onUserJoined callback uid {0} {1}", uid, elapsed);
            Debug.Log(userJoinedMessage);
            //RTMManager.instance.SendPeerMessage(member.GetUserId());
        };

        mRtcEngine.OnUserOffline += (uint uid, USER_OFFLINE_REASON reason) =>
        {
            string userOfflineMessage = string.Format("onUserOffline callback uid {0} {1}", uid, reason);
            Debug.Log(userOfflineMessage);

            if (RTMManager.instance.activeSpeakers.Contains(uid))
            {
                RTMManager.instance.activeSpeakers.Remove(uid);
                Debug.Log("error check" + ":" + RTMManager.instance.activeSpeakers.Contains(uid));

                Destroy(RTMManager.instance.speakerObjects[RTMManager.instance.userIds[uid]]);
                RTMManager.instance.speakerObjects.Remove(RTMManager.instance.userIds[uid]);
                Debug.Log(RTMManager.instance.speakerObjects.ContainsKey(RTMManager.instance.userIds[uid]));
            }
            if (RTMManager.instance.activeAudience.Contains(uid))
            {
                RTMManager.instance.activeAudience.Remove(uid);

                Destroy(RTMManager.instance.audienceObjects[RTMManager.instance.userIds[uid]]);
                RTMManager.instance.audienceObjects.Remove(RTMManager.instance.userIds[uid]);
            }
            if (RTMManager.instance.userIds.ContainsKey(uid))
            {
                RTMManager.instance.userIds.Remove(uid);
            }

        };

        mRtcEngine.OnVolumeIndication += (AudioVolumeInfo[] speakers, int speakerNumber, int totalVolume) =>
        {
            

            for (int idx = 0; idx < speakerNumber; idx++)
            {
                string volumeIndicationMessage = string.Format("{0} onVolumeIndication {1} {2}", speakerNumber, speakers[idx].uid, speakers[idx].volume);
                //Debug.Log(volumeIndicationMessage);
                if (speakers[idx].uid == 0)
                {
                    
                    if (speakers[idx].volume > 5)
                    {
                        if (prefabHolder != null)
                        {
                            Debug.Log(string.Format("onVolumeIndication only local {0}", totalVolume));
                            prefabHolder.GetComponent<Image>().color = color;
                        }

                    }
                    else
                    {
                        if (prefabHolder != null)
                        {
                            prefabHolder.GetComponent<Image>().color = defcolor;
                        }
                    }
                }
                else
                {
                    if (speakers[idx].volume > 5)
                    {
                        if (RTMManager.instance.speakerObjects.ContainsKey(RTMManager.instance.userIds[speakers[idx].uid]))
                        {
                            RTMManager.instance.speakerObjects[RTMManager.instance.userIds[speakers[idx].uid]].GetComponent<Image>().color = color;
                        }

                    }
                    else
                    {
                        if (RTMManager.instance.speakerObjects.ContainsKey(RTMManager.instance.userIds[speakers[idx].uid]))
                        {
                            RTMManager.instance.speakerObjects[RTMManager.instance.userIds[speakers[idx].uid]].GetComponent<Image>().color = defcolor;
                        }
                    }
                }
                
            }
        };

            mRtcEngine.OnUserMutedAudio += (uint uid, bool muted) =>
            {
                string userMutedMessage = string.Format("onUserMuted callback uid {0} {1}", uid, muted);
                Debug.Log(userMutedMessage);
            };

            mRtcEngine.OnConnectionInterrupted += () =>
            {
                string interruptedMessage = string.Format("OnConnectionInterrupted");
                Debug.Log(interruptedMessage);
            };

            mRtcEngine.OnConnectionLost += () =>
            {
                string lostMessage = string.Format("OnConnectionLost");
                Debug.Log(lostMessage);
            };
            mRtcEngine.OnRemoteAudioStateChanged += (uint uid, REMOTE_AUDIO_STATE state, REMOTE_AUDIO_STATE_REASON reason, int elapsed) =>
            {
                print(state + ":" + reason);
                if (state == REMOTE_AUDIO_STATE.REMOTE_AUDIO_STATE_DECODING || state == REMOTE_AUDIO_STATE.REMOTE_AUDIO_STATE_STARTING)
                {

                    Debug.Log(RTMManager.instance.speakerObjects.ContainsKey(RTMManager.instance.userIds[uid]));
                    if (!RTMManager.instance.speakerObjects.ContainsKey(RTMManager.instance.userIds[uid]))
                    {
                        Debug.Log(RTMManager.instance.speakerObjects.ContainsKey(RTMManager.instance.userIds[uid]));

                        if (RTMManager.instance.activeAudience.Contains(uid))
                        {
                            RTMManager.instance.activeAudience.Remove(uid);

                            Destroy(RTMManager.instance.audienceObjects[RTMManager.instance.userIds[uid]]);
                            RTMManager.instance.audienceObjects.Remove(RTMManager.instance.userIds[uid]);
                        }

                        //RTMManager.instance.activeSpeakers.Remove(uid);
                        //Destroy(RTMManager.instance.speakerObjects[RTMManager.instance.userIds[uid]]);
                        //RTMManager.instance.speakerObjects.Remove(RTMManager.instance.userIds[uid]);
                        RTMManager.instance.activeSpeakers.Add(uid);
                        GameObject obj = Instantiate(RTMManager.instance.peoplePrefab);
                        obj.transform.parent = RTMManager.instance.SpeakerParent.transform;
                        obj.GetComponentInChildren<Text>().text = RTMManager.instance.userIds[uid];
                        obj.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
                        obj.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, 0);
                        RTMManager.instance.speakerObjects.Add(RTMManager.instance.userIds[uid], obj);
                    }

                }
                else if (state == REMOTE_AUDIO_STATE.REMOTE_AUDIO_STATE_FAILED || state == REMOTE_AUDIO_STATE.REMOTE_AUDIO_STATE_STOPPED)
                {
                    //Debug.Log(RTMManager.instance.speakerObjects.ContainsKey(RTMManager.instance.userIds[uid]));
                    if (!(reason == REMOTE_AUDIO_STATE_REASON.REMOTE_AUDIO_REASON_REMOTE_OFFLINE))
                    {
                        if (!RTMManager.instance.audienceObjects.ContainsKey(RTMManager.instance.userIds[uid]))
                        {
                            Debug.Log(RTMManager.instance.speakerObjects.ContainsKey(RTMManager.instance.userIds[uid]));

                            if (RTMManager.instance.activeSpeakers.Contains(uid))
                            {
                                RTMManager.instance.activeSpeakers.Remove(uid);
                                Debug.Log("error check" + ":" + RTMManager.instance.activeSpeakers.Contains(uid));

                                Destroy(RTMManager.instance.speakerObjects[RTMManager.instance.userIds[uid]]);
                                RTMManager.instance.speakerObjects.Remove(RTMManager.instance.userIds[uid]);
                                Debug.Log(RTMManager.instance.speakerObjects.ContainsKey(RTMManager.instance.userIds[uid]));
                            }
                            // RTMManager.instance.activeSpeakers.Remove(uid);
                            //Destroy(RTMManager.instance.speakerObjects[RTMManager.instance.userIds[uid]]);
                            //RTMManager.instance.speakerObjects.Remove(RTMManager.instance.userIds[uid]);
                            RTMManager.instance.activeAudience.Add(uid);
                            GameObject obj = Instantiate(RTMManager.instance.peoplePrefab);
                            obj.transform.parent = RTMManager.instance.AudienceParent.transform;
                            obj.GetComponentInChildren<Text>().text = RTMManager.instance.userIds[uid];
                            obj.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
                            obj.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, 0);
                            RTMManager.instance.audienceObjects.Add(RTMManager.instance.userIds[uid], obj);
                        }
                    }
                }

            };
            mRtcEngine.OnLocalAudioStateChanged += (LOCAL_AUDIO_STREAM_STATE state, LOCAL_AUDIO_STREAM_ERROR error) =>
            {
                //Debug.Log(state);
                if (state == LOCAL_AUDIO_STREAM_STATE.LOCAL_AUDIO_STREAM_STATE_ENCODING || state == LOCAL_AUDIO_STREAM_STATE.LOCAL_AUDIO_STREAM_STATE_RECORDING)
                {
                    if (prefabHolder != null)
                    {
                        Destroy(prefabHolder);
                        prefabHolder = null;
                    }
                    prefabHolder = Instantiate(RTMManager.instance.peoplePrefab);
                    prefabHolder.transform.parent = RTMManager.instance.SpeakerParent.transform;
                    prefabHolder.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
                    prefabHolder.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, 0);
                    prefabHolder.GetComponentInChildren<Text>().text = RTMManager.instance.UserName;
                    /*RTMManager.instance.activeAudience.Remove(uid);
                    RTMManager.instance.activeSpeakers.Add(uid);
                    Destroy(RTMManager.instance.audienceObjects[RTMManager.instance.userIds[uid]]);
                    RTMManager.instance.audienceObjects.Remove(RTMManager.instance.userIds[uid]);
                    GameObject obj = Instantiate(RTMManager.instance.peoplePrefab);
                    obj.transform.parent = RTMManager.instance.SpeakerParent.transform;
                    obj.GetComponentInChildren<Text>().text = RTMManager.instance.userIds[uid];
                    RTMManager.instance.speakerObjects.Add(RTMManager.instance.userIds[uid], obj);*/
                }
                else if (state == LOCAL_AUDIO_STREAM_STATE.LOCAL_AUDIO_STREAM_STATE_FAILED || state == LOCAL_AUDIO_STREAM_STATE.LOCAL_AUDIO_STREAM_STATE_STOPPED)
                {
                    if (prefabHolder != null)
                    {
                        Destroy(prefabHolder);
                        prefabHolder = null;
                    }
                    prefabHolder = Instantiate(RTMManager.instance.peoplePrefab);
                    prefabHolder.transform.parent = RTMManager.instance.AudienceParent.transform;
                    prefabHolder.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
                    prefabHolder.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, 0);
                    prefabHolder.GetComponentInChildren<Text>().text = RTMManager.instance.UserName;
                    /* RTMManager.instance.activeSpeakers.Remove(uid);
                     Destroy(RTMManager.instance.speakerObjects[RTMManager.instance.userIds[uid]]);
                     RTMManager.instance.speakerObjects.Remove(RTMManager.instance.userIds[uid]);
                     GameObject obj = Instantiate(RTMManager.instance.peoplePrefab);
                     obj.transform.parent = RTMManager.instance.AudienceParent.transform;
                     obj.GetComponentInChildren<Text>().text = RTMManager.instance.userIds[uid];
                     RTMManager.instance.speakerObjects.Add(RTMManager.instance.userIds[uid], obj);*/
                }
            };
            //mRtcEngine.SetLogFilter(LOG_FILTER.INFO);

            // mRtcEngine.SetChannelProfile(CHANNEL_PROFILE.CHANNEL_PROFILE_LIVE_BROADCASTING);

            mRtcEngine.SetChannelProfile(CHANNEL_PROFILE.CHANNEL_PROFILE_LIVE_BROADCASTING);
            mRtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        mRtcEngine.EnableAudioVolumeIndication(300, 3, true);
        }
        void Start()
        {
            instance = this;

            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
            //joinChannel.onClick.AddListener(JoinChannel);
            leaveChannel.onClick.AddListener(LeaveChannel);
            muteButton.onClick.AddListener(MuteButtonTapped);
            StartFunction();
        ColorUtility.TryParseHtmlString("#15D417", out color);
        ColorUtility.TryParseHtmlString("#FFFFFF", out defcolor);

        }
        public void JoinChannel()
        {
            //string channelName = RTMManager.instance.ChannelName.Trim();
            string channelName = PlayerPrefs.GetString("RTM_CHANNEL", "");

            Debug.Log(string.Format("tap joinChannel with channel name {0}", channelName));

            if (string.IsNullOrEmpty(channelName))
            {
                return;
            }

            mRtcEngine.JoinChannel(channelName, "extra", 0);
        }

        public void LeaveChannel()
        {
            mRtcEngine.LeaveChannel();
            string channelName = RTMManager.instance.ChannelName;
            Debug.Log(string.Format("left channel name {0}", channelName));
        }

        /*void OnApplicationQuit()
        {
            if (mRtcEngine != null)
            {
                IRtcEngine.Destroy();
            }
        }*/


        bool isMuted = false;
        void MuteButtonTapped()
        {
            string labeltext = isMuted ? "Mute" : "Unmute";
            Text label = muteButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = labeltext;
            }
            isMuted = !isMuted;
            mRtcEngine.EnableLocalAudio(!isMuted);
        }
}
