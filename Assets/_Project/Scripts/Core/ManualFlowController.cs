// -----------------------------------------------------------------------------
//  ManualFlowController.cs
//  DECRYPTED - A Walk Through the History of Secret Writing
//
//  Drives room-to-room progression when the player is playing by hand (Demo Mode
//  OFF). The DemoDirector only runs in Demo Mode, which left manual play with no
//  way out of the transition rooms (Splash, Atrium) and no safety net if a puzzle
//  could not be solved. This controller guarantees the player can always move
//  forward:
//
//    Splash  : advance when PLAY is pressed, or after a short safety timeout.
//    Atrium  : advance after a short look-around dwell.
//    Puzzle  : the player solves it to advance (the normal path). If they are
//              still stuck after a generous grace period, the exhibit is solved
//              for them so the experience never dead-ends.
//    Reveal  : the FinalRevealController completes the experience itself.
//
//  It is inert while Demo Mode is on, so it never fights the DemoDirector.
// -----------------------------------------------------------------------------

using System.Collections;
using Decrypted.Interaction;
using UnityEngine;

namespace Decrypted.Core
{
    [DisallowMultipleComponent]
    public class ManualFlowController : MonoBehaviour
    {
        [Header("Pacing (seconds)")]
        [Tooltip("Safety: if the player never presses PLAY, start anyway after this.")]
        [SerializeField] private float _splashSafety = 9f;
        [Tooltip("Time to take in the atrium before moving to the first exhibit.")]
        [SerializeField] private float _atriumDwell = 8f;
        [Tooltip("Grace period in a puzzle room before it is solved for the player " +
                 "so the experience never dead-ends.")]
        [SerializeField] private float _puzzleGrace = 120f;

        private GameManager _gm;
        private bool _splashHandled;

        private void OnEnable()
        {
            EventBus.Subscribe<StateChangedEvent>(OnStateChanged);
            EventBus.Subscribe<RoomEnteredEvent>(OnRoomEntered);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<StateChangedEvent>(OnStateChanged);
            EventBus.Unsubscribe<RoomEnteredEvent>(OnRoomEntered);
        }

        private bool Manual => GameManager.Instance != null && !GameManager.Instance.DemoMode;

        private void OnStateChanged(StateChangedEvent e)
        {
            if (e.Current == MuseumState.Splash && !_splashHandled && Manual)
            {
                _splashHandled = true;
                StartCoroutine(SplashSafety());
            }
        }

        private IEnumerator SplashSafety()
        {
            yield return new WaitForSeconds(_splashSafety);
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == MuseumState.Splash)
                EventBus.Publish(new ExperienceStartedEvent());
        }

        private void OnRoomEntered(RoomEnteredEvent e)
        {
            if (!Manual) return;
            _gm = GameManager.Instance;

            switch (e.Room)
            {
                case MuseumState.Atrium:
                    StartCoroutine(AdvanceAfter(_atriumDwell, MuseumState.Atrium));
                    break;
                case MuseumState.AncientRoom:
                case MuseumState.WWIIRoom:
                case MuseumState.VaultRoom:
                    StartCoroutine(PuzzleGrace(e.Room));
                    break;
            }
        }

        private IEnumerator AdvanceAfter(float delay, MuseumState room)
        {
            yield return new WaitForSeconds(delay);
            if (_gm != null && _gm.CurrentState == room) _gm.Advance();
        }

        // If the player has not solved the puzzle after the grace period, solve it
        // for them so they can keep going. Solving raises the normal solved event,
        // which the GameManager turns into the room transition.
        private IEnumerator PuzzleGrace(MuseumState room)
        {
            yield return new WaitForSeconds(_puzzleGrace);
            if (_gm == null || _gm.CurrentState != room || _gm.IsSolved(room)) yield break;

            switch (room)
            {
                case MuseumState.AncientRoom:
                    FindObjectOfType<CaesarCipherController>(true)?.AutoSolve();
                    break;
                case MuseumState.WWIIRoom:
                    var enigma = FindObjectOfType<EnigmaController>(true);
                    if (enigma != null) StartCoroutine(enigma.AutoSolve(0.15f, true));
                    break;
                case MuseumState.VaultRoom:
                    var vault = FindObjectOfType<VaultKeypad>(true);
                    if (vault != null) StartCoroutine(vault.AutoEnter(0.12f));
                    break;
            }
        }
    }
}
