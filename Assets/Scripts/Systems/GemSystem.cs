using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ubongo.Systems
{
    /// <summary>
    /// 보석 타입 열거형
    /// </summary>
    public enum GemType
    {
        Ruby,       // 루비 - 4점
        Sapphire,   // 사파이어 - 3점
        Emerald,    // 에메랄드 - 2점
        Amber       // 앰버 - 1점
    }

    /// <summary>
    /// 개별 보석 데이터 클래스 (불변)
    /// </summary>
    [Serializable]
    public class Gem
    {
        public GemType Type { get; }
        public int PointValue { get; }
        public Color Color { get; }

        public Gem(GemType type)
        {
            Type = type;
            PointValue = GetPointValue(type);
            Color = GetColor(type);
        }

        private static int GetPointValue(GemType type)
        {
            return type switch
            {
                GemType.Ruby => 4,
                GemType.Sapphire => 3,
                GemType.Emerald => 2,
                GemType.Amber => 1,
                _ => 0
            };
        }

        private static Color GetColor(GemType type)
        {
            return type switch
            {
                GemType.Ruby => new Color(0.9f, 0.1f, 0.1f),      // 빨강
                GemType.Sapphire => new Color(0.1f, 0.3f, 0.9f),  // 파랑
                GemType.Emerald => new Color(0.1f, 0.8f, 0.2f),   // 초록
                GemType.Amber => new Color(1f, 0.75f, 0.2f),      // 호박색
                _ => Color.white
            };
        }
    }

    /// <summary>
    /// 보석 컬렉션 (불변 패턴)
    /// </summary>
    [Serializable]
    public class GemCollection
    {
        private readonly List<Gem> _gems;

        public IReadOnlyList<Gem> Gems => _gems.AsReadOnly();

        public int TotalPoints => _gems.Sum(g => g.PointValue);

        public int TotalCount => _gems.Count;

        public GemCollection()
        {
            _gems = new List<Gem>();
        }

        private GemCollection(List<Gem> gems)
        {
            _gems = new List<Gem>(gems);
        }

        /// <summary>
        /// 보석을 추가한 새로운 컬렉션 반환 (불변)
        /// </summary>
        public GemCollection AddGem(Gem gem)
        {
            var newGems = new List<Gem>(_gems) { gem };
            return new GemCollection(newGems);
        }

        /// <summary>
        /// 여러 보석을 추가한 새로운 컬렉션 반환 (불변)
        /// </summary>
        public GemCollection AddGems(IEnumerable<Gem> gems)
        {
            var newGems = new List<Gem>(_gems);
            newGems.AddRange(gems);
            return new GemCollection(newGems);
        }

        /// <summary>
        /// 타입별 보석 개수 반환
        /// </summary>
        public int GetCountByType(GemType type)
        {
            return _gems.Count(g => g.Type == type);
        }

        /// <summary>
        /// 타입별 포인트 합계 반환
        /// </summary>
        public int GetPointsByType(GemType type)
        {
            return _gems.Where(g => g.Type == type).Sum(g => g.PointValue);
        }
    }

    /// <summary>
    /// 보석 획득 결과 (불변)
    /// </summary>
    public readonly struct GemRewardResult
    {
        public Gem FixedGem { get; }
        public Gem RandomGem { get; }
        public bool HasFixedGem => FixedGem != null;
        public bool HasRandomGem => RandomGem != null;
        public int TotalPoints => (FixedGem?.PointValue ?? 0) + (RandomGem?.PointValue ?? 0);

        public GemRewardResult(Gem fixedGem, Gem randomGem)
        {
            FixedGem = fixedGem;
            RandomGem = randomGem;
        }

        public IEnumerable<Gem> GetAllGems()
        {
            if (HasFixedGem) yield return FixedGem;
            if (HasRandomGem) yield return RandomGem;
        }
    }

    /// <summary>
    /// 보석 시스템 - 보석 획득 로직 관리
    /// </summary>
    public class GemSystem : MonoBehaviour
    {
        private static GemSystem _instance;
        public static GemSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GemSystem>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GemSystem");
                        _instance = go.AddComponent<GemSystem>();
                    }
                }
                return _instance;
            }
        }

        private GemCollection _playerGems;
        private readonly System.Random _random = new System.Random();

        public event Action<GemRewardResult> OnGemsAwarded;
        public event Action<GemCollection> OnGemCollectionChanged;

        public GemCollection PlayerGems => _playerGems;
        public int TotalPoints => _playerGems?.TotalPoints ?? 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeGemCollection();
        }

        private void InitializeGemCollection()
        {
            _playerGems = new GemCollection();
        }

        /// <summary>
        /// 보석 컬렉션 초기화 (새 게임 시작 시)
        /// </summary>
        public void ResetGemCollection()
        {
            _playerGems = new GemCollection();
            OnGemCollectionChanged?.Invoke(_playerGems);
        }

        /// <summary>
        /// 싱글플레이어 보석 획득 - 남은 시간 비율에 따라 보상 결정
        /// </summary>
        /// <param name="remainingTimeRatio">남은 시간 / 총 시간 (0.0 ~ 1.0)</param>
        /// <returns>획득한 보석 결과</returns>
        public GemRewardResult AwardGemsForSinglePlayer(float remainingTimeRatio)
        {
            if (remainingTimeRatio < 0f)
            {
                return new GemRewardResult(null, null);
            }

            Gem fixedGem = DetermineFixedGemByTime(remainingTimeRatio);
            Gem randomGem = remainingTimeRatio > 0.25f ? GenerateRandomGem() : null;

            var result = new GemRewardResult(fixedGem, randomGem);
            ApplyReward(result);

            return result;
        }

        /// <summary>
        /// 멀티플레이어 보석 획득 - 순위에 따라 보상 결정
        /// </summary>
        /// <param name="rank">완료 순위 (1-4)</param>
        /// <param name="completed">퍼즐 완료 여부</param>
        /// <returns>획득한 보석 결과</returns>
        public GemRewardResult AwardGemsForMultiplayer(int rank, bool completed)
        {
            if (!completed)
            {
                return new GemRewardResult(null, null);
            }

            Gem fixedGem = DetermineFixedGemByRank(rank);
            Gem randomGem = GenerateRandomGem();

            var result = new GemRewardResult(fixedGem, randomGem);
            ApplyReward(result);

            return result;
        }

        private Gem DetermineFixedGemByTime(float remainingTimeRatio)
        {
            if (remainingTimeRatio > 0.75f)
            {
                return new Gem(GemType.Ruby);
            }
            if (remainingTimeRatio > 0.50f)
            {
                return new Gem(GemType.Sapphire);
            }
            if (remainingTimeRatio > 0.25f)
            {
                return new Gem(GemType.Emerald);
            }
            if (remainingTimeRatio > 0f)
            {
                return new Gem(GemType.Amber);
            }
            return null;
        }

        private Gem DetermineFixedGemByRank(int rank)
        {
            return rank switch
            {
                1 => new Gem(GemType.Sapphire),  // 1등: 사파이어 (3점)
                2 => new Gem(GemType.Amber),     // 2등: 앰버 (1점)
                _ => null                         // 3등, 4등: 고정 보석 없음
            };
        }

        private Gem GenerateRandomGem()
        {
            var gemTypes = Enum.GetValues(typeof(GemType));
            var randomType = (GemType)gemTypes.GetValue(_random.Next(gemTypes.Length));
            return new Gem(randomType);
        }

        private void ApplyReward(GemRewardResult result)
        {
            var gemsToAdd = result.GetAllGems().ToList();
            if (gemsToAdd.Count > 0)
            {
                _playerGems = _playerGems.AddGems(gemsToAdd);
                OnGemsAwarded?.Invoke(result);
                OnGemCollectionChanged?.Invoke(_playerGems);
            }
        }

        /// <summary>
        /// 플레이어 등급 계산 (싱글플레이어)
        /// </summary>
        public char CalculateGrade()
        {
            int totalPoints = TotalPoints;

            if (totalPoints >= 36) return 'S';
            if (totalPoints >= 27) return 'A';
            if (totalPoints >= 18) return 'B';
            if (totalPoints >= 9) return 'C';
            return 'D';
        }

        /// <summary>
        /// 보석 컬렉션 요약 정보 반환
        /// </summary>
        public GemCollectionSummary GetCollectionSummary()
        {
            return new GemCollectionSummary(
                rubyCount: _playerGems.GetCountByType(GemType.Ruby),
                sapphireCount: _playerGems.GetCountByType(GemType.Sapphire),
                emeraldCount: _playerGems.GetCountByType(GemType.Emerald),
                amberCount: _playerGems.GetCountByType(GemType.Amber),
                totalPoints: _playerGems.TotalPoints
            );
        }
    }

    /// <summary>
    /// 보석 컬렉션 요약 (불변)
    /// </summary>
    public readonly struct GemCollectionSummary
    {
        public int RubyCount { get; }
        public int SapphireCount { get; }
        public int EmeraldCount { get; }
        public int AmberCount { get; }
        public int TotalPoints { get; }
        public int TotalGems => RubyCount + SapphireCount + EmeraldCount + AmberCount;

        public GemCollectionSummary(int rubyCount, int sapphireCount, int emeraldCount, int amberCount, int totalPoints)
        {
            RubyCount = rubyCount;
            SapphireCount = sapphireCount;
            EmeraldCount = emeraldCount;
            AmberCount = amberCount;
            TotalPoints = totalPoints;
        }
    }
}
