using System;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    public class RockPaperScissorsMenuRef : MenuReference
    {
        public TextMeshProUGUI opponentNameText;
        public BasicTMPButton button1;
        public BasicTMPButton button2;
        public BasicTMPButton button3;
        public BasicTMPButton quitButton;
        public TextMeshProUGUI resultText;
        public TextMeshProUGUI playerScoreText;
        public TextMeshProUGUI opponentScoreText;
        public TextMeshProUGUI connectionStatusText;
    }
}
