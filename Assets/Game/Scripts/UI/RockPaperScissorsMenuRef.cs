using System;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    public class RockPaperScissorsMenuRef : MenuReference
    {
        public TextMeshPro opponentNameText;
        public BasicTMPButton button1;
        public BasicTMPButton button2;
        public BasicTMPButton button3;
        public BasicTMPButton quitButton;
        public TextMeshPro resultText;
        public TextMeshPro playerScoreText;
        public TextMeshPro opponentScoreText;
        public TextMeshPro connectionStatusText;
    }
}
