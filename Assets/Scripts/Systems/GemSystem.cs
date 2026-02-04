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
    /// 보석 풀 모드 열거형
    /// </summary>
    public enum GemPoolMode
    {
        Infinite,   // 무한 모드 (기본) - 보석 소진 없음
        Finite,     // 유한 모드 - 58개 보석 풀
        Classic     // 클래식 모드 - 유한 + 엄격한 원본 규칙
    }

    /// <summary>
    /// 유한 보석 풀 구조체 (불변 패턴)
    /// 원본 Ubongo 3D: 총 58개 보석
    /// </summary>
    [Serializable]
    public struct GemPool
    {
        public int Rubies { get; }      // 초기: 12
        public int Sapphires { get; }   // 초기: 12
        public int Emeralds { get; }    // 초기: 16
        public int Ambers { get; }      // 초기: 18

        public int TotalRemaining => Rubies + Sapphires + Emeralds + Ambers;
        public bool IsEmpty => TotalRemaining == 0;

        public GemPool(int rubies, int sapphires, int emeralds, int ambers)
        {
            Rubies = rubies;
            Sapphires = sapphires;
            Emeralds = emeralds;
            Ambers = ambers;
        }

        public static GemPool CreateDefault() => new GemPool(
            rubies: 12,
            sapphires: 12,
            emeralds: 16,
            ambers: 18
        );

        public static GemPool CreateEmpty() => new GemPool(0, 0, 0, 0);

        /// <summary>
        /// 특정 타입의 보석 수량 조회
        /// </summary>
        public int GetCount(GemType type)
        {
            return type switch
            {
                GemType.Ruby => Rubies,
                GemType.Sapphire => Sapphires,
                GemType.Emerald => Emeralds,
                GemType.Amber => Ambers,
                _ => 0
            };
        }

        /// <summary>
        /// 특정 타입의 보석이 있는지 확인
        /// </summary>
        public bool HasGem(GemType type) => GetCount(type) > 0;

        /// <summary>
        /// 보석 1개 차감한 새 풀 반환 (불변)
        /// </summary>
        public GemPool WithDrawn(GemType type)
        {
            return type switch
            {
                GemType.Ruby => new GemPool(Rubies - 1, Sapphires, Emeralds, Ambers),
                GemType.Sapphire => new GemPool(Rubies, Sapphires - 1, Emeralds, Ambers),
                GemType.Emerald => new GemPool(Rubies, Sapphires, Emeralds - 1, Ambers),
                GemType.Amber => new GemPool(Rubies, Sapphires, Emeralds, Ambers - 1),
                _ => this
            };
        }
    }

    /// <summary>
    /// 보석 풀 관리자 - 유한 보석 추출 로직
    /// </summary>
    public class GemPoolManager
    {
        private GemPool _pool;
        private readonly System.Random _random;

        public GemPool CurrentPool => _pool;
        public bool IsEmpty => _pool.IsEmpty;

        public event Action<GemPool> OnPoolChanged;

        public GemPoolManager()
        {
            _pool = GemPool.CreateDefault();
            _random = new System.Random();
        }

        /// <summary>
        /// 풀 초기화
        /// </summary>
        public void Reset()
        {
            _pool = GemPool.CreateDefault();
            OnPoolChanged?.Invoke(_pool);
        }

        /// <summary>
        /// 랜덤 보석 추출 (가중치 기반)
        /// </summary>
        public GemType? DrawRandomGem()
        {
            if (_pool.IsEmpty) return null;

            int total = _pool.TotalRemaining;
            int roll = _random.Next(total);

            // 가중치 기반 선택 (남은 수량에 비례)
            if (roll < _pool.Rubies)
            {
                _pool = _pool.WithDrawn(GemType.Ruby);
                OnPoolChanged?.Invoke(_pool);
                return GemType.Ruby;
            }
            roll -= _pool.Rubies;

            if (roll < _pool.Sapphires)
            {
                _pool = _pool.WithDrawn(GemType.Sapphire);
                OnPoolChanged?.Invoke(_pool);
                return GemType.Sapphire;
            }
            roll -= _pool.Sapphires;

            if (roll < _pool.Emeralds)
            {
                _pool = _pool.WithDrawn(GemType.Emerald);
                OnPoolChanged?.Invoke(_pool);
                return GemType.Emerald;
            }

            _pool = _pool.WithDrawn(GemType.Amber);
            OnPoolChanged?.Invoke(_pool);
            return GemType.Amber;
        }

        /// <summary>
        /// 특정 타입의 보석 추출
        /// </summary>
        public bool DrawSpecificGem(GemType type)
        {
            if (!_pool.HasGem(type)) return false;

            _pool = _pool.WithDrawn(type);
            OnPoolChanged?.Invoke(_pool);
            return true;
        }

        /// <summary>
        /// 특정 타입 또는 대체 타입 보석 추출
        /// 요청 타입이 소진된 경우 다음 높은 가치의 보석으로 대체
        /// </summary>
        public GemType? DrawSpecificOrFallback(GemType preferredType)
        {
            if (_pool.HasGem(preferredType))
            {
                _pool = _pool.WithDrawn(preferredType);
                OnPoolChanged?.Invoke(_pool);
                return preferredType;
            }

            // 대체 순서: Ruby > Sapphire > Emerald > Amber
            GemType[] fallbackOrder = { GemType.Ruby, GemType.Sapphire, GemType.Emerald, GemType.Amber };

            foreach (var fallback in fallbackOrder)
            {
                if (_pool.HasGem(fallback))
                {
                    _pool = _pool.WithDrawn(fallback);
                    OnPoolChanged?.Invoke(_pool);
                    return fallback;
                }
            }

            return null;
        }
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
                    _instance = FindAnyObjectByType<GemSystem>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GemSystem");
                        _instance = go.AddComponent<GemSystem>();
                    }
                }
                return _instance;
            }
        }

        [Header("Gem Pool Settings")]
        [SerializeField] private GemPoolMode poolMode = GemPoolMode.Infinite;

        private GemCollection _playerGems;
        private GemPoolManager _poolManager;
        private readonly System.Random _random = new System.Random();

        public event Action<GemRewardResult> OnGemsAwarded;
        public event Action<GemCollection> OnGemCollectionChanged;
        public event Action<GemPool> OnGemPoolChanged;
        public event Action<GemPoolMode> OnPoolModeChanged;

        public GemCollection PlayerGems => _playerGems;
        public int TotalPoints => _playerGems?.TotalPoints ?? 0;
        public GemPoolMode PoolMode => poolMode;
        public GemPool CurrentPool => _poolManager?.CurrentPool ?? GemPool.CreateDefault();
        public bool IsPoolEmpty => poolMode != GemPoolMode.Infinite && (_poolManager?.IsEmpty ?? false);

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
            InitializeGemPool();
        }

        private void InitializeGemCollection()
        {
            _playerGems = new GemCollection();
        }

        private void InitializeGemPool()
        {
            _poolManager = new GemPoolManager();
            _poolManager.OnPoolChanged += HandlePoolChanged;
        }

        private void HandlePoolChanged(GemPool pool)
        {
            OnGemPoolChanged?.Invoke(pool);
        }

        /// <summary>
        /// 보석 풀 모드 설정
        /// </summary>
        public void SetPoolMode(GemPoolMode mode)
        {
            poolMode = mode;
            OnPoolModeChanged?.Invoke(poolMode);
        }

        /// <summary>
        /// 보석 컬렉션 초기화 (새 게임 시작 시)
        /// </summary>
        public void ResetGemCollection()
        {
            _playerGems = new GemCollection();
            _poolManager?.Reset();
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
            GemType targetType;

            if (remainingTimeRatio > 0.75f)
            {
                targetType = GemType.Ruby;
            }
            else if (remainingTimeRatio > 0.50f)
            {
                targetType = GemType.Sapphire;
            }
            else if (remainingTimeRatio > 0.25f)
            {
                targetType = GemType.Emerald;
            }
            else if (remainingTimeRatio > 0f)
            {
                targetType = GemType.Amber;
            }
            else
            {
                return null;
            }

            return GenerateSpecificGem(targetType);
        }

        private Gem DetermineFixedGemByRank(int rank)
        {
            GemType? targetType = rank switch
            {
                1 => GemType.Sapphire,  // 1등: 사파이어 (3점)
                2 => GemType.Amber,     // 2등: 앰버 (1점)
                _ => null               // 3등, 4등: 고정 보석 없음
            };

            if (!targetType.HasValue) return null;

            return GenerateSpecificGem(targetType.Value);
        }

        private Gem GenerateRandomGem()
        {
            // Finite/Classic 모드: 풀에서 추출
            if (poolMode != GemPoolMode.Infinite)
            {
                var drawnType = _poolManager?.DrawRandomGem();
                return drawnType.HasValue ? new Gem(drawnType.Value) : null;
            }

            // Infinite 모드: 무한 생성
            var gemTypes = Enum.GetValues(typeof(GemType));
            var randomType = (GemType)gemTypes.GetValue(_random.Next(gemTypes.Length));
            return new Gem(randomType);
        }

        /// <summary>
        /// 특정 타입의 보석 생성 (풀 모드 지원)
        /// </summary>
        private Gem GenerateSpecificGem(GemType type)
        {
            // Finite/Classic 모드: 풀에서 추출
            if (poolMode != GemPoolMode.Infinite)
            {
                var drawnType = _poolManager?.DrawSpecificOrFallback(type);
                return drawnType.HasValue ? new Gem(drawnType.Value) : null;
            }

            // Infinite 모드: 직접 생성
            return new Gem(type);
        }

        /// <summary>
        /// Second Chance Round용 랜덤 보석만 지급
        /// </summary>
        public GemRewardResult AwardRandomGemOnly()
        {
            Gem randomGem = GenerateRandomGem();
            var result = new GemRewardResult(null, randomGem);
            ApplyReward(result);
            return result;
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
