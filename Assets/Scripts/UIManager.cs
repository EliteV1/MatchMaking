using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Main Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registrationPanel;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Sub Panels")]
    [SerializeField] private GameObject friendsPanel;
    [SerializeField] private GameObject matchmakingPanel;

    private void Awake()
    {
        CreateInstance();
    }

    private void CreateInstance()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    // Methods to open specific panels
    public void OpenLoginPanel()
    {
        CloseAllPanels();
        loginPanel.SetActive(true);
    }

    public void OpenRegistrationPanel()
    {
        CloseAllPanels();
        registrationPanel.SetActive(true);
    }

    public void OpenMainMenuPanel()
    {
        CloseAllPanels();
        mainMenuPanel.SetActive(true);
    }

    public void OpenFriendsPanel()
    {
        CloseAllPanels();
        friendsPanel.SetActive(true);
    }

    public void OpenMatchmakingPanel()
    {
        CloseAllPanels();
        matchmakingPanel.SetActive(true);
    }

    // Helper method to close all panels
    private void CloseAllPanels()
    {
        loginPanel.SetActive(false);
        registrationPanel.SetActive(false);
        mainMenuPanel.SetActive(false);
        friendsPanel.SetActive(false);
        matchmakingPanel.SetActive(false);
    }
}
