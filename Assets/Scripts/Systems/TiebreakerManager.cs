using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ubongo.Systems
{
    /// <summary>
    /// 타이브레이커 상태 열거형
    /// </summary>
    public enum TiebreakerState
    {
        NotActive,          // 타이브레이커 비활성
        Preparing,          // 준비 중 (동점자 확인, 퍼즐 생성)
        InProgress,         // 타이브레이커 진행 중 (타이머 없음)
        WinnerDetermined,   // 승자 결정됨
        AdditionalRound     // 추가 라운드 필요 (동시 완성)
    }

    /// <summary>
    /// 타이브레이커 플레이어 데이터 (불변)
    /// </summary>
    public readonly struct TiebreakerPlayer
    {
        public int PlayerId { get; }
        public string PlayerName { get; }
        public int GemPoints { get; }
        public int GemCount { get; }

        public TiebreakerPlayer(int playerId, string playerName, int gemPoints, int gemCount)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            GemPoints = gemPoints;
            GemCount = gemCount;
        }
    }

    /// <summary>
    /// 타이브레이커 결과 (불변)
    /// </summary>
    public readonly struct TiebreakerResult
    {
        public int WinnerId { get; }
        public string WinnerName { get; }
        public float CompletionTime { get; }
        public int RoundsPlayed { get; }
        public bool IsCoDraw { get; }

        public TiebreakerResult(int winnerId, string winnerName, float completionTime, int roundsPlayed, bool isCoDraw = false)
        {
            WinnerId = winnerId;
            WinnerName = winnerName;
            CompletionTime = completionTime;
            RoundsPlayed = roundsPlayed;
            IsCoDraw = isCoDraw;
        }

        public static TiebreakerResult CreateCoDraw(int roundsPlayed) =>
            new TiebreakerResult(-1, "", 0f, roundsPlayed, true);
    }

    /// <summary>
    /// 타이브레이커 관리자 - 동점 상황 처리
    /// 원본 Ubongo 3D 규칙: 동점 시 타이머 없이 퍼즐 대결, 첫 완성자 승리
    /// </summary>
    public class TiebreakerManager : MonoBehaviour
    {
        private static TiebreakerManager _instance;
        public static TiebreakerManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<TiebreakerManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("TiebreakerManager");
                        _instance = go.AddComponent<TiebreakerManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Tiebreaker Settings")]
        [SerializeField] private int maxAdditionalRounds = 3;
        [SerializeField] private float preparationDelay = 2f;

        private TiebreakerState _currentState = TiebreakerState.NotActive;
        private List<TiebreakerPlayer> _tiedPlayers;
        private float _tiebreakerStartTime;
        private int _currentTiebreakerRound;
        private Dictionary<int, float> _completionTimes;

        // Events
        public event Action<List<TiebreakerPlayer>> OnTiebreakerStarting;
        public event Action OnTiebreakerStarted;
        public event Action<int, float> OnPlayerCompleted;  // playerId, time
        public event Action<TiebreakerResult> OnTiebreakerEnded;
        public event Action OnAdditionalRoundStarting;

        // Properties
        public TiebreakerState CurrentState => _currentState;
        public IReadOnlyList<TiebreakerPlayer> TiedPlayers => _tiedPlayers?.AsReadOnly();
        public int CurrentTiebreakerRound => _currentTiebreakerRound;
        public bool IsActive => _currentState != TiebreakerState.NotActive;
        public float ElapsedTime => _currentState == TiebreakerState.InProgress
            ? Time.time - _tiebreakerStartTime
            : 0f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // DontDestroyOnLoad은 루트 오브젝트에만 적용 가능
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            _tiedPlayers = new List<TiebreakerPlayer>();
            _completionTimes = new Dictionary<int, float>();
            _currentState = TiebreakerState.NotActive;
            _currentTiebreakerRound = 0;
        }

        /// <summary>
        /// 동점 상황 확인 및 타이브레이커 필요 여부 반환
        /// </summary>
        public bool CheckForTie(List<(int playerId, string name, int gemPoints, int gemCount)> playerResults)
        {
            if (playerResults == null || playerResults.Count < 2)
                return false;

            // 최고 점수 찾기
            int maxPoints = playerResults.Max(p => p.gemPoints);

            // 최고 점수 플레이어들 필터링
            var topPlayers = playerResults.Where(p => p.gemPoints == maxPoints).ToList();

            if (topPlayers.Count < 2)
                return false;

            // 보석 개수로 2차 비교
            int maxGemCount = topPlayers.Max(p => p.gemCount);
            var stillTied = topPlayers.Where(p => p.gemCount == maxGemCount).ToList();

            if (stillTied.Count < 2)
                return false;

            // 타이브레이커 필요
            _tiedPlayers = stillTied.Select(p =>
                new TiebreakerPlayer(p.playerId, p.name, p.gemPoints, p.gemCount)
            ).ToList();

            return true;
        }

        /// <summary>
        /// 타이브레이커 시작
        /// </summary>
        public void StartTiebreaker()
        {
            if (_tiedPlayers == null || _tiedPlayers.Count < 2)
            {
                Debug.LogWarning("[TiebreakerManager] Cannot start tiebreaker: insufficient tied players");
                return;
            }

            _currentState = TiebreakerState.Preparing;
            _currentTiebreakerRound = 1;
            _completionTimes.Clear();

            OnTiebreakerStarting?.Invoke(_tiedPlayers);

            StartCoroutine(PrepareTiebreakerRound());
        }

        private IEnumerator PrepareTiebreakerRound()
        {
            yield return new WaitForSeconds(preparationDelay);

            _currentState = TiebreakerState.InProgress;
            _tiebreakerStartTime = Time.time;

            OnTiebreakerStarted?.Invoke();
        }

        /// <summary>
        /// 플레이어 퍼즐 완료 등록
        /// 타이브레이커에서는 첫 번째 완료자가 승리
        /// </summary>
        public void RegisterPlayerCompletion(int playerId)
        {
            if (_currentState != TiebreakerState.InProgress)
                return;

            // 이미 완료한 플레이어인지 확인
            if (_completionTimes.ContainsKey(playerId))
                return;

            // 참가자인지 확인
            if (!_tiedPlayers.Any(p => p.PlayerId == playerId))
                return;

            float completionTime = Time.time - _tiebreakerStartTime;
            _completionTimes[playerId] = completionTime;

            OnPlayerCompleted?.Invoke(playerId, completionTime);

            // 첫 완료자 확인
            if (_completionTimes.Count == 1)
            {
                // 동시 완성 감지를 위해 짧은 대기
                StartCoroutine(CheckForSimultaneousCompletion(playerId, completionTime));
            }
        }

        private IEnumerator CheckForSimultaneousCompletion(int firstPlayerId, float firstTime)
        {
            // 밀리초 단위 동시 완성 감지 (50ms 윈도우)
            yield return new WaitForSeconds(0.05f);

            // 동시 완성 확인
            var simultaneousCompletions = _completionTimes
                .Where(kvp => Mathf.Abs(kvp.Value - firstTime) < 0.05f)
                .ToList();

            if (simultaneousCompletions.Count > 1)
            {
                // 동시 완성 - 추가 라운드 필요
                HandleSimultaneousCompletion();
            }
            else
            {
                // 단독 승자
                DetermineWinner(firstPlayerId, firstTime);
            }
        }

        private void HandleSimultaneousCompletion()
        {
            if (_currentTiebreakerRound >= maxAdditionalRounds)
            {
                // 최대 라운드 도달 - 공동 우승
                _currentState = TiebreakerState.WinnerDetermined;
                OnTiebreakerEnded?.Invoke(TiebreakerResult.CreateCoDraw(_currentTiebreakerRound));
                return;
            }

            // 추가 라운드
            _currentState = TiebreakerState.AdditionalRound;
            _currentTiebreakerRound++;
            _completionTimes.Clear();

            OnAdditionalRoundStarting?.Invoke();

            StartCoroutine(PrepareTiebreakerRound());
        }

        private void DetermineWinner(int winnerId, float completionTime)
        {
            _currentState = TiebreakerState.WinnerDetermined;

            var winner = _tiedPlayers.FirstOrDefault(p => p.PlayerId == winnerId);

            var result = new TiebreakerResult(
                winnerId: winnerId,
                winnerName: winner.PlayerName,
                completionTime: completionTime,
                roundsPlayed: _currentTiebreakerRound
            );

            OnTiebreakerEnded?.Invoke(result);
        }

        /// <summary>
        /// 타이브레이커 취소/리셋
        /// </summary>
        public void Reset()
        {
            StopAllCoroutines();
            Initialize();
        }

        /// <summary>
        /// 타이브레이커 참가자인지 확인
        /// </summary>
        public bool IsParticipant(int playerId)
        {
            return _tiedPlayers?.Any(p => p.PlayerId == playerId) ?? false;
        }

        /// <summary>
        /// 현재 완료한 플레이어 수 반환
        /// </summary>
        public int GetCompletedPlayerCount()
        {
            return _completionTimes.Count;
        }
    }
}
