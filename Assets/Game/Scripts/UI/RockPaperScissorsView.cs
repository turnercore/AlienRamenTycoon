// View for the Rock Paper Scissors Game, this class handles the UI elements and user interactions.
// Will track connection status, if connected to the game, send actions, wait for results, and update scores.
using System;
using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    public class RockPaperScissorsView : IApplicationLifecycle
    {
        private readonly GameObject rockPaperScissorsMenuPrefab;
        private RockPaperScissorsMenuRef rockPaperScissorsMenuRef;

        // map of which button corresponds to which action
        private readonly Dictionary<BasicTMPButton, RockPaperScissorsAction> buttonActionMap;
        public event Action OnQuitRequested;
        public event Action<RockPaperScissorsAction> OnActionSubmitted;
        public bool initalized;

        public RockPaperScissorsView(GameObject rockPaperScissorsMenuPrefab)
        {
            this.rockPaperScissorsMenuPrefab = rockPaperScissorsMenuPrefab;
            buttonActionMap = new Dictionary<BasicTMPButton, RockPaperScissorsAction>();
        }

        public enum RockPaperScissorsAction
        {
            Rock,
            Paper,
            Scissors,
        }

        public void Initialize()
        {
            if (initalized)
                return;

            rockPaperScissorsMenuRef = GameObject
                .Instantiate(rockPaperScissorsMenuPrefab)
                .GetComponent<RockPaperScissorsMenuRef>();
            AddListeners();
            RandomizeButtons();

            initalized = true;
        }

        private void AddListeners()
        {
            // Add listeners for the buttons in the menu reference
            rockPaperScissorsMenuRef.button1.Button.onClick.AddListener(OnButton1Clicked);
            rockPaperScissorsMenuRef.button2.Button.onClick.AddListener(OnButton2Clicked);
            rockPaperScissorsMenuRef.button3.Button.onClick.AddListener(OnButton3Clicked);
            rockPaperScissorsMenuRef.quitButton.Button.onClick.AddListener(OnQuitClicked);
        }

        private void OnQuitClicked()
        {
            // Quit clicked, bubble to the owning game mode
            OnQuitRequested?.Invoke();
        }

        public void Tick() { }

        public void Dispose()
        // cleanup self, listeners, and dispose any children that need to be disposed.
        {
            if (rockPaperScissorsMenuRef != null)
            {
                rockPaperScissorsMenuRef.button1.Button.onClick.RemoveListener(OnButton1Clicked);
                rockPaperScissorsMenuRef.button2.Button.onClick.RemoveListener(OnButton2Clicked);
                rockPaperScissorsMenuRef.button3.Button.onClick.RemoveListener(OnButton3Clicked);
                rockPaperScissorsMenuRef.quitButton.Button.onClick.RemoveListener(OnQuitClicked);
                GameObject.Destroy(rockPaperScissorsMenuRef.gameObject);
                rockPaperScissorsMenuRef = null;
            }
        }

        private void OnButton3Clicked()
        {
            // Make sure the button is mapped
            if (!buttonActionMap.ContainsKey(rockPaperScissorsMenuRef.button3))
            {
                Debug.LogError("Button 3 not mapped to an action!");
                return;
            }

            // For now, output in log which action they picked:
            Debug.Log(
                "Button 3 clicked - action: " + buttonActionMap[rockPaperScissorsMenuRef.button3]
            );
            SubmitAction(buttonActionMap[rockPaperScissorsMenuRef.button3]);
        }

        private void OnButton1Clicked()
        {
            //Make sure the button is mapped
            if (!buttonActionMap.ContainsKey(rockPaperScissorsMenuRef.button1))
            {
                Debug.LogError("Button 1 not mapped to an action!");
                return;
            }

            Debug.Log(
                "Button 1 clicked - action: " + buttonActionMap[rockPaperScissorsMenuRef.button1]
            );
            SubmitAction(buttonActionMap[rockPaperScissorsMenuRef.button1]);
        }

        private void OnButton2Clicked()
        {
            //Make sure the button is mapped
            if (!buttonActionMap.ContainsKey(rockPaperScissorsMenuRef.button2))
            {
                Debug.LogError("Button 2 not mapped to an action!");
                return;
            }

            Debug.Log(
                "Button 2 clicked - action: " + buttonActionMap[rockPaperScissorsMenuRef.button2]
            );
            SubmitAction(buttonActionMap[rockPaperScissorsMenuRef.button2]);
        }

        private void RandomizeButtons()
        {
            List<RockPaperScissorsAction> actions = new List<RockPaperScissorsAction>
            {
                RockPaperScissorsAction.Rock,
                RockPaperScissorsAction.Paper,
                RockPaperScissorsAction.Scissors,
            };

            System.Random rand = new System.Random();
            int n = actions.Count;
            while (n > 1)
            {
                int k = rand.Next(n--);
                RockPaperScissorsAction temp = actions[n];
                actions[n] = actions[k];
                actions[k] = temp;
            }

            // Update button texts and mappings
            UpdateButtonText(rockPaperScissorsMenuRef.button1, actions[0].ToString());
            UpdateButtonMappings(rockPaperScissorsMenuRef.button1, actions[0]);
            UpdateButtonText(rockPaperScissorsMenuRef.button2, actions[1].ToString());
            UpdateButtonMappings(rockPaperScissorsMenuRef.button2, actions[1]);
            UpdateButtonText(rockPaperScissorsMenuRef.button3, actions[2].ToString());
            UpdateButtonMappings(rockPaperScissorsMenuRef.button3, actions[2]);
        }

        private void UpdateButtonMappings(BasicTMPButton tmpButton, RockPaperScissorsAction action)
        {
            if (buttonActionMap.ContainsKey(tmpButton))
            {
                buttonActionMap[tmpButton] = action;
            }
            else
            {
                buttonActionMap.Add(tmpButton, action);
            }
        }

        public void UpdateConnectionStatus(string status)
        {
            rockPaperScissorsMenuRef.connectionStatusText.text = status;
        }

        public void UpdateOpponentName(string name)
        {
            rockPaperScissorsMenuRef.opponentNameText.text = name;
        }

        public void UpdateScores(int playerScore, int opponentScore)
        {
            rockPaperScissorsMenuRef.playerScoreText.text = $"Player Score: {playerScore}";
            rockPaperScissorsMenuRef.opponentScoreText.text = $"Opponent Score: {opponentScore}";
        }

        public void UpdateRound(int round)
        {
            rockPaperScissorsMenuRef.roundNumberText.text = $"Round: {round}";
        }

        public void DisplayResult(string result)
        {
            rockPaperScissorsMenuRef.resultText.text = result;
        }

        public void SetActionButtonsVisible(bool isVisible)
        {
            rockPaperScissorsMenuRef.button1.gameObject.SetActive(isVisible);
            rockPaperScissorsMenuRef.button2.gameObject.SetActive(isVisible);
            rockPaperScissorsMenuRef.button3.gameObject.SetActive(isVisible);
        }

        public void ShowWaitingForOpponent()
        {
            DisplayResult("Waiting for opponent...");
        }

        private void UpdateButtonText(BasicTMPButton button, string text)
        {
            button.TMPText.text = text;
        }

        // Submit Action to Server
        private void SubmitAction(RockPaperScissorsAction action)
        {
            // Placeholder for submitting action to server logic
            Debug.Log("Submitting action to server: " + action);
            OnActionSubmitted?.Invoke(action);
        }
    }
}
