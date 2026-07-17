using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SimpleJRPG.Demo
{
    public class DemoHub : MonoBehaviour
    {
        public string classicTurnScene;
        public string atbTurnScene;
        public string timelineTurnScene;
        public string pressTurnScene;
        public string actionPointScene;

        public Button btnClassicTurn;
        public Button btnATBTurn;
        public Button btnTimelineTurn;
        public Button btnPressTurn;
        public Button btnActionPoint;

        void Start()
        {
            if (btnClassicTurn != null)
                btnClassicTurn.onClick.AddListener(() => LoadScene(classicTurnScene));
            if (btnATBTurn != null)
                btnATBTurn.onClick.AddListener(() => LoadScene(atbTurnScene));
            if (btnTimelineTurn != null)
                btnTimelineTurn.onClick.AddListener(() => LoadScene(timelineTurnScene));
            if (btnPressTurn != null)
                btnPressTurn.onClick.AddListener(() => LoadScene(pressTurnScene));
            if (btnActionPoint != null)
                btnActionPoint.onClick.AddListener(() => LoadScene(actionPointScene));
        }

        private void LoadScene(string sceneName)
        {
            if (!string.IsNullOrEmpty(sceneName))
                SceneManager.LoadScene(sceneName);
        }
    }
}
