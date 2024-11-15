using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Database;

public class FirebaseAuthManager : MonoBehaviour
{
   
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public FirebaseUser user;
    public FirebaseFirestore db;

    // Login Variables
    [Space]
    [Header("Login")]
    public InputField emailLoginField;
    public InputField passwordLoginField;

    // Registration Variables
    [Space]
    [Header("Registration")]
    public InputField nameRegisterField;
    public InputField emailRegisterField;
    public InputField passwordRegisterField;
    public InputField confirmPasswordRegisterField;

    // UI Elements for Users List
    [Space]
    [Header("UI Elements for Users List")]
    public Transform usersListContainer; 
    public GameObject userItemPrefab;    

    public FirebaseDatabase database;
    public GameObject friendRequestItemPrefab;
    public Transform friendRequestsListContainer;

    private void Awake()
    {
        
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Could not resolve all firebase dependencies: " + dependencyStatus);
            }
        });
    }

    void InitializeFirebase()
    {
        
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        database = FirebaseDatabase.DefaultInstance; 

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    
    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

            if (!signedIn && user != null)
            {
                
                UpdateUserConnectionStatus(user.UserId, false);
                Debug.Log("Signed out " + user.UserId);
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                
                UpdateUserConnectionStatus(user.UserId, true);
                Debug.Log("Signed in " + user.UserId);
            }
        }
    }

    public void Login()
    {
        StartCoroutine(LoginAsync(emailLoginField.text, passwordLoginField.text));
    }

    private IEnumerator LoginAsync(string email, string password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError(loginTask.Exception);

            FirebaseException firebaseException = loginTask.Exception.GetBaseException() as FirebaseException;
            AuthError authError = (AuthError)firebaseException.ErrorCode;

            string failedMessage = "Login Failed! Because ";

            switch (authError)
            {
                case AuthError.InvalidEmail:
                    failedMessage += "Email is invalid";
                    break;
                case AuthError.WrongPassword:
                    failedMessage += "Wrong Password";
                    break;
                case AuthError.MissingEmail:
                    failedMessage += "Email is missing";
                    break;
                case AuthError.MissingPassword:
                    failedMessage += "Password is missing";
                    break;
                default:
                    failedMessage = "Login Failed";
                    break;
            }

            Debug.Log(failedMessage);
        }
        else
        {
            Debug.LogFormat("{0} You Are Successfully Logged In", user.DisplayName);
            References.userName = user.DisplayName;
            
        }
    }

    public void Register()
    {
        StartCoroutine(RegisterAsync(nameRegisterField.text, emailRegisterField.text, passwordRegisterField.text, confirmPasswordRegisterField.text));
    }

    private IEnumerator RegisterAsync(string name, string email, string password, string confirmPassword)
    {
        if (name == "")
        {
            Debug.LogError("User Name is empty");
        }
        else if (email == "")
        {
            Debug.LogError("Email field is empty");
        }
        else if (passwordRegisterField.text != confirmPasswordRegisterField.text)
        {
            Debug.LogError("Password does not match");
        }
        else
        {
            var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

            yield return new WaitUntil(() => registerTask.IsCompleted);

            if (registerTask.Exception != null)
            {
                Debug.LogError(registerTask.Exception);

                FirebaseException firebaseException = registerTask.Exception.GetBaseException() as FirebaseException;
                AuthError authError = (AuthError)firebaseException.ErrorCode;

                string failedMessage = "Registration Failed! Because ";
                switch (authError)
                {
                    case AuthError.InvalidEmail:
                        failedMessage += "Email is invalid";
                        break;
                    case AuthError.WrongPassword:
                        failedMessage += "Wrong Password";
                        break;
                    case AuthError.MissingEmail:
                        failedMessage += "Email is missing";
                        break;
                    case AuthError.MissingPassword:
                        failedMessage += "Password is missing";
                        break;
                    default:
                        failedMessage = "Registration Failed";
                        break;
                }

                Debug.Log(failedMessage);
            }
            else
            {
                UserProfile userProfile = new UserProfile { DisplayName = name };
                var updateProfileTask = user.UpdateUserProfileAsync(userProfile);

                yield return new WaitUntil(() => updateProfileTask.IsCompleted);

                if (updateProfileTask.Exception != null)
                {
                    user.DeleteAsync();
                    Debug.LogError(updateProfileTask.Exception);
                }
                else
                {
                    Debug.Log("Registration Successful! Welcome " + user.DisplayName);
                    UIManager.Instance.OpenLoginPanel();
                }
            }
        }
    }

    public void FetchUsers()
    {
        db.Collection("users").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                QuerySnapshot usersSnapshot = task.Result;
                foreach (DocumentSnapshot document in usersSnapshot.Documents)
                {
                    string userName = document.GetValue<string>("displayName");
                    string userEmail = document.GetValue<string>("email");
                    Debug.Log("displayName");

                    GameObject userItem = Instantiate(userItemPrefab, usersListContainer);
                    userItem.GetComponentInChildren<Text>().text = userName;

                    Button friendRequestButton = userItem.GetComponentInChildren<Button>();
                    friendRequestButton.onClick.AddListener(() => SendFriendRequest(document.Id));
                }
            }
            else
            {
                Debug.LogError("Error fetching users: " + task.Exception);
            }
        });
    }
    public void FetchOnlineUsers()
    {
        DatabaseReference usersRef = database.GetReference("users");

        usersRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                foreach (DataSnapshot userSnapshot in snapshot.Children)
                {
                    string userId = userSnapshot.Key;
                    string status = userSnapshot.Child("status").Value.ToString();

                    if (status == "online")
                    {
                        string userName = userSnapshot.Child("displayName").Value.ToString();

                        GameObject userItem = Instantiate(userItemPrefab, usersListContainer);
                        userItem.GetComponentInChildren<Text>().text = userName;

                        Button friendRequestButton = userItem.GetComponentInChildren<Button>();
                        friendRequestButton.onClick.AddListener(() => SendFriendRequest(userId));
                    }
                }
            }
            else
            {
                Debug.LogError("Error fetching online users: " + task.Exception);
            }
        });
    }

    public void SendFriendRequest(string userId)
    {
        DatabaseReference friendRequestRef = database.GetReference("friend_requests").Push();
        friendRequestRef.Child("fromUser").SetValueAsync(user.UserId);
        friendRequestRef.Child("toUser").SetValueAsync(userId);

        Debug.Log("Friend request sent to: " + userId);
    }
    void UpdateUserConnectionStatus(string userId, bool isConnected)
    {
        DatabaseReference userStatusRef = database.GetReference("users/" + userId + "/status");
        userStatusRef.SetValueAsync(isConnected ? "online" : "offline");
    }
    private void CreateMatchmakingRoom(string userId)
    {
        DatabaseReference newRoomRef = database.GetReference("matchmaking_rooms").Push();
        newRoomRef.Child("player1").SetValueAsync(userId);
    }
    public void AcceptMatchmakingRequest(string requestId, string opponentUserId)
    {
        CreateMatchmakingRoom(opponentUserId);

        
        DatabaseReference requestRef = database.GetReference("matchmaking_requests/" + requestId);
        requestRef.RemoveValueAsync();
    }
    void StartListeningForFriendRequests()
    {
        DatabaseReference friendRequestRef = database.GetReference("friend_requests");
        friendRequestRef.ChildAdded += HandleNewFriendRequest;
    }

    void HandleNewFriendRequest(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        
        string fromUserId = args.Snapshot.Child("fromUser").Value.ToString();
        Debug.Log("New friend request from: " + fromUserId);
    }
    public void ListenForFriendRequests()
    {
        DatabaseReference friendRequestRef = database.GetReference("friend_requests");

        friendRequestRef.ChildAdded += (sender, args) =>
        {
            string fromUserId = args.Snapshot.Child("fromUser").Value.ToString();
            string requestId = args.Snapshot.Key;

            GameObject friendRequestItem = Instantiate(friendRequestItemPrefab, friendRequestsListContainer);
            friendRequestItem.GetComponentInChildren<Text>().text = "Solicitud de: " + fromUserId;

            Button acceptButton = friendRequestItem.transform.Find("AcceptButton").GetComponent<Button>();
            Button declineButton = friendRequestItem.transform.Find("DeclineButton").GetComponent<Button>();

            acceptButton.onClick.AddListener(() => AcceptFriendRequest(requestId, fromUserId));
            declineButton.onClick.AddListener(() => DeclineFriendRequest(requestId));
        };

    }
    public void AcceptFriendRequest(string requestId, string fromUserId)
    {
        
        DatabaseReference requestRef = database.GetReference("friend_requests/" + requestId);
        requestRef.RemoveValueAsync();
    }

    public void DeclineFriendRequest(string requestId)
    {
       
        DatabaseReference requestRef = database.GetReference("friend_requests/" + requestId);
        requestRef.RemoveValueAsync();
    }
    public void StartMatchmaking()
    {
       

        DatabaseReference matchmakingRoomRef = database.GetReference("matchmaking_rooms");

        matchmakingRoomRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.ChildrenCount == 0)
            {
                
                CreateMatchmakingRoom(user.UserId);
              
            }
            else
            {
                foreach (DataSnapshot roomSnapshot in task.Result.Children)
                {
                    if (roomSnapshot.Child("player2").Value == null)
                    {
                        
                        string roomId = roomSnapshot.Key;
                        JoinMatchmakingRoom(roomId);
                       
                        break;
                    }
                }
            }
        });
    }
    private void JoinMatchmakingRoom(string roomId)
    {
        DatabaseReference roomRef = database.GetReference("matchmaking_rooms/" + roomId);
        roomRef.Child("player2").SetValueAsync(user.UserId);
    }
}