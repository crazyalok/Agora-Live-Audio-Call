using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using agora_rtm;
using System;
using agora_gaming_rtc;

public class RTMManager : MonoBehaviour
{
    public static RTMManager instance;
    [SerializeField]
    private string appId = "";
    // [SerializeField]
    private string token = "";

    [SerializeField] InputField userNameInput, channelNameInput;
    //[SerializeField] InputField channelMsgInputBox;
    //[SerializeField] InputField peerMessageBox;

    //[SerializeField] MessageDisplay messageDisplay;

    private RtmClient rtmClient = null;
    private RtmChannel channel;
    private RtmCallManager callManager;

    private RtmClientEventHandler clientEventHandler;
    private RtmChannelEventHandler channelEventHandler;
    private RtmCallEventHandler callEventHandler;
    public GameObject RTCManagerObj;

    string _userName = "";
    public Dictionary<uint, string> userIds;
    public Dictionary<string, GameObject> audienceObjects, speakerObjects;
    [HideInInspector]
    public List<uint> activeSpeakers;
    [HideInInspector]
    public List<uint> activeAudience;
    public GameObject AudienceParent, SpeakerParent, peoplePrefab;
    public GameObject groupLoginUI;
    [HideInInspector]
    public List<string> pendingsrequest;
    public string UserName {
        get { return _userName; }
        set {
            _userName = value;
            PlayerPrefs.SetString("RTM_USER", _userName);
            PlayerPrefs.Save();
        }
    }

    string _channelName = "";
    public string ChannelName
    {
        get { return _channelName; }
        set {
            _channelName = value;
            PlayerPrefs.SetString("RTM_CHANNEL", _channelName);
            PlayerPrefs.Save();
        }
    }

    agora_rtm.SendMessageOptions _MessageOptions = new agora_rtm.SendMessageOptions() {
        enableOfflineMessaging = true,
        enableHistoricalMessaging = true
    };

    private void Awake()
    {
        userNameInput.text = PlayerPrefs.GetString("RTM_USER", "");
        channelNameInput.text = PlayerPrefs.GetString("RTM_CHANNEL", "");
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        clientEventHandler = new RtmClientEventHandler();
        channelEventHandler = new RtmChannelEventHandler();
        callEventHandler = new RtmCallEventHandler();

        rtmClient = new RtmClient(appId, clientEventHandler);
        clientEventHandler.OnLoginSuccess = OnClientLoginSuccessHandler;
        clientEventHandler.OnLoginFailure = OnClientLoginFailureHandler;
        clientEventHandler.OnMessageReceivedFromPeer = OnMessageReceivedFromPeerHandler;

        channelEventHandler.OnJoinSuccess = OnJoinSuccessHandler;
        channelEventHandler.OnJoinFailure = OnJoinFailureHandler;
        channelEventHandler.OnLeave = OnLeaveHandler;
        channelEventHandler.OnMessageReceived = OnChannelMessageReceivedHandler;

        // Optional, tracking members
        channelEventHandler.OnGetMembers = OnGetMembersHandler;
        channelEventHandler.OnMemberCountUpdated = OnMemberCountUpdatedHandler;
        channelEventHandler.OnMemberJoined = OnMemberJoinedHandler;
        channelEventHandler.OnMemberLeft = OnMemberLeftHandler;

        callManager = rtmClient.GetRtmCallManager(callEventHandler);
        // state
        clientEventHandler.OnConnectionStateChanged = OnConnectionStateChangedHandler;
        userIds = new Dictionary<uint, string>();
        audienceObjects = new Dictionary<string, GameObject>();
        speakerObjects = new Dictionary<string, GameObject>();
        pendingsrequest = new List<string>();
        activeSpeakers = new List<uint>();
        activeAudience = new List<uint>();
    }

    void OnApplicationQuit()
    {
        if (RTCManager.instance != null)
        {
            IRtcEngine.Destroy();
        }
        if (channel != null)
        {
            channel.Dispose();
            channel = null;
        }
        if (rtmClient != null)
        {
            rtmClient.Dispose();
            rtmClient = null;
        }
    }
    public void Login()
    {
        UserName = userNameInput.text;

        if (string.IsNullOrEmpty(UserName))
        {
            Debug.LogError("We need a username and appId to login");
            return;
        }

        rtmClient.Login(token, UserName);
    }

    public void Logout()
    {
        //messageDisplay.AddTextToDisplay(UserName + " logged out of the rtm", Message.MessageType.Info);
        rtmClient.Logout();
    }

    public void ChannelMemberCountButtonPressed()
    {
        if (channel != null)
        {
            channel.GetMembers();
        }
    }

    public void JoinChannel()
    {
        ChannelName = channelNameInput.GetComponent<InputField>().text;
        channel = rtmClient.CreateChannel(ChannelName, channelEventHandler);
        ShowCurrentChannelName();
        channel.Join();
        
    }

    public void LeaveChannel()
    {
        //messageDisplay.AddTextToDisplay(UserName + " left the chat", Message.MessageType.Info);
        channel.Leave();
    }

    public void SendMessageToChannel()
    {
        /*string msg = channelMsgInputBox.text;
        string peer = "[channel:" + ChannelName + "]";

        string displayMsg = string.Format("{0}->{1}: {2}", UserName, peer, msg);

        //messageDisplay.AddTextToDisplay(displayMsg, Message.MessageType.PlayerMessage);
        channel.SendMessage(rtmClient.CreateMessage(msg));*/
    }

    public void SendPeerMessage(string peerid)
    {

        string msg = RTCManager.instance.selfuserid.ToString();
        string peer = peerid;


        rtmClient.SendMessageToPeer(
            peerId: peer,
            message: rtmClient.CreateMessage(msg),
            options: _MessageOptions
        );
        
        //peerMessageBox.text = "";
    }

    void ShowCurrentChannelName()
    {
        ChannelName = channelNameInput.GetComponent<InputField>().text;
        Debug.Log("Channel name is " + ChannelName);
    }

    void OnJoinSuccessHandler(int id)
    {
        string msg = "channel:" + ChannelName + " OnJoinSuccess id = " + id;
        Debug.Log(msg);
        
        channel.GetMembers();
        RTCManagerObj.SetActive(true);
        //messageDisplay.AddTextToDisplay(msg, Message.MessageType.Info);
        if(RTCManager.instance != null)
        {
            RTCManager.instance.StartFunction();
        }
        //UploadImageButton.interactable = true;
    }

    void OnJoinFailureHandler(int id, JOIN_CHANNEL_ERR errorCode)
    {
        string msg = "channel OnJoinFailure  id = " + id + " errorCode = " + errorCode;
        Debug.Log(msg);
        //messageDisplay.AddTextToDisplay(msg, Message.MessageType.Error);
    }

    void OnClientLoginSuccessHandler(int id)
    {
        string msg = "client login successful! id = " + id;
        Debug.Log(msg);
        groupLoginUI.SetActive(false);
        JoinChannel();
        //messageDisplay.AddTextToDisplay(msg, Message.MessageType.Info);
    }

    void OnClientLoginFailureHandler(int id, LOGIN_ERR_CODE errorCode)
    {
        string msg = "client login unsuccessful! id = " + id + " errorCode = " + errorCode;
        Debug.Log(msg);
        
        // messageDisplay.AddTextToDisplay(msg, Message.MessageType.Error);
    }
    public void Finish()
    {
        
        Logout();
        groupLoginUI.SetActive(true);
        RTCManager.instance.groupRTCUI.SetActive(false);
        RTCManager.instance.peopleList.SetActive(false);
        //RTCManager.instance = null;
        RTCManagerObj.SetActive(false);
    }

    void OnLeaveHandler(int id, LEAVE_CHANNEL_ERR errorCode)
    {
        string msg = "client onleave id = " + id + " errorCode = " + errorCode;
        Debug.Log(msg);
        // messageDisplay.AddTextToDisplay(msg, Message.MessageType.Info);
    }

    void OnChannelMessageReceivedHandler(int id, string userId, TextMessage message)
    {
        Debug.Log("client OnChannelMessageReceived id = " + id + ", from user:" + userId + " text:" + message.GetText());
        // messageDisplay.AddTextToDisplay(userId + ": " + message.GetText(), Message.MessageType.ChannelMessage);
    }

    void OnGetMembersHandler(int id, RtmChannelMember[] members, int userCount, GET_MEMBERS_ERR errorCode)
    {
        if (errorCode == GET_MEMBERS_ERR.GET_MEMBERS_ERR_OK)
        {
            // messageDisplay.AddTextToDisplay("Total members = " + userCount, Message.MessageType.Info);
            Debug.Log("Total members = " + userCount);
            foreach(RtmChannelMember rtmChannelMember in members)
            {
                Debug.Log(rtmChannelMember.GetUserId());
                if (!rtmChannelMember.GetUserId().Equals(UserName))
                {
                    pendingsrequest.Add(rtmChannelMember.GetUserId());
                }
            }
        } else {
            // messageDisplay.AddTextToDisplay("something is wrong with GetMembers:" + errorCode.ToString(), Message.MessageType.Error);
        }
        RTCManagerObj.SetActive(true);
    }

    void OnMessageReceivedFromPeerHandler(int id, string peerId, TextMessage message)
    {
        var temp = Convert.ToUInt32(message.GetText());
        print(temp);
        userIds.Add(temp, peerId);
        activeAudience.Add(temp);
        //instantiate prefabs
        GameObject obj = Instantiate(peoplePrefab);
        obj.transform.parent = AudienceParent.transform;
        obj.GetComponentInChildren<Text>().text = peerId;
        obj.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
        obj.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, 0);
        audienceObjects.Add(peerId, obj);

        Debug.Log("client OnMessageReceivedFromPeer id = " + id + ", from user:" + peerId + " text:" + message.GetText());
        // messageDisplay.AddTextToDisplay(peerId + ": " + message.GetText(), Message.MessageType.PeerMessage);
    }

    void OnMemberCountUpdatedHandler(int id, int memberCount)
    {
        Debug.Log("Member count changed to:" + memberCount);
    }
    void OnMemberJoinedHandler(int id, RtmChannelMember member)
    {
        string msg = "channel OnMemberJoinedHandler member ID=" + member.GetUserId() + " channelId = " + member.GetChannelId();
        Debug.Log(msg);
       // GameObject obj = Instantiate(peoplePrefab);
       // obj.transform.parent = SpeakerParent.transform;
       // obj.GetComponentInChildren<Text>().text = member.GetUserId();
        //speakerObjects.Add(member.GetUserId(), obj);
        SendPeerMessage(member.GetUserId());
        //messageDisplay.AddTextToDisplay(msg, Message.MessageType.Info);
    }

    void OnMemberLeftHandler(int id, RtmChannelMember member)
    {
        string msg = "channel OnMemberLeftHandler member ID=" + member.GetUserId() + " channelId = " + member.GetChannelId();
        Debug.Log(msg);
        //messageDisplay.AddTextToDisplay(msg, Message.MessageType.Info);
    }


    void OnSendMessageResultHandler(int id, long messageId, PEER_MESSAGE_ERR_CODE errorCode)
    {
        string msg = string.Format("Sent message with id:{0} MessageId:{1} errorCode:{2}", id, messageId, errorCode);
        Debug.Log(msg);
        // messageDisplay.AddTextToDisplay(msg, errorCode == PEER_MESSAGE_ERR_CODE.PEER_MESSAGE_ERR_OK ? Message.MessageType.Info : Message.MessageType.Error);
    }
    void OnConnectionStateChangedHandler(int id, CONNECTION_STATE state, CONNECTION_CHANGE_REASON reason)
    {
        string msg = string.Format("connection state changed id:{0} state:{1} reason:{2}", id, state, reason);
        Debug.Log(msg);
        //messageDisplay.AddTextToDisplay(msg, Message.MessageType.Info);
    }
}
