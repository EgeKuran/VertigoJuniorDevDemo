using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public enum PlayerTeam
{
    None,
    BlueTeam,

    RedTeam
}

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private List<SpawnPoint> _sharedSpawnPoints = new List<SpawnPoint>();
    System.Random _random = new System.Random();
    float _closestDistance;
    [Tooltip("This will be used to calculate the second filter where algorithm looks for closest friends, if the friends are away from this value, they will be ignored")]
    [SerializeField] private float _maxDistanceToClosestFriend = 30;
    [Tooltip("This will be used to calculate the first filter where algorithm looks for enemies that are far away from this value. Only enemies which are away from this value will be calculated.")]
    [SerializeField] private float _minDistanceToClosestEnemy = 10;
    [Tooltip("This value is to prevent friendly player spawning on top of eachothers. If a player is within the range of this value to a spawn point, that spawn point will be ignored")]
    [SerializeField] private float _minMemberDistance = 2;


    public DummyPlayer PlayerToBeSpawned;
    public DummyPlayer[] DummyPlayers;

    
    /// <summary>
    /// Awake de paylaşımlı spawn noktaları bulunarak _sharedSpawnPoints listesine eklenir.
    /// DummyPlayer tipindeki oyuncular bulunarak DummyPlayer dizisine atanır.
    /// </summary>
    private void Awake()
    {
		_sharedSpawnPoints.AddRange(FindObjectsOfType<SpawnPoint>());
        Debug.Log("Paylaşımlı Spawn Noktaları Listesi oluşturuldu.");

		DummyPlayers = FindObjectsOfType<DummyPlayer>();
        Debug.Log("Test oyuncuları listesi oluşturuldu.");
    }

    #region SPAWN ALGORITHM


    /// <summary>
    /// Paylaşımlı spawn noktalarından en uygununu döndürür.
    /// spawnPoints listesi oluşturulur.
    /// Tüm spawn noktaları için en yakın düşman ve en yakın arkadaş uzaklıkları hesaplanır.
    /// Düşman oyuncuların uzaklıklarına göre en uygun spawn noktaları bulunur.
    /// Eğer uygun spawn noktası bulunamadıysa arkadaş oyuncuların uzaklıklarına göre uygun spawn noktalarına bakılır.
    /// SpawnPoint değişkenine en uygun spawn noktası atanır ve o spawn noktasının timer'ı başlatılır.
    ///     Bulunan uygun spawn noktalarının sayısı 1'e küçük eşitken spawnPoints listesinin ilk elemanı spawnPoint e atanır.
    ///     GetSpawnPointsByDistanceSpawning metodunda uygun spawn noktası bulunamaması halinde paylaşımlı spawn noktaları 
    ///         listesinin ilk elemanı döndürüldüğü için hiç bir zaman spawnPoint.Count =0 olmaz.
    ///     Eğer bulunan uygun spawn noktalarının sayısı 1 den fazlaysa listenin yarısına kadar rastgele bir spawn noktası döndürülür.
    /// 
    /// </summary>
    
    public SpawnPoint GetSharedSpawnPoint(PlayerTeam team)
    {
        List<SpawnPoint> spawnPoints = new List<SpawnPoint>(_sharedSpawnPoints.Count);
        CalculateDistancesForSpawnPoints(team);
        Debug.Log("-------------------------------------------------------------");
        GetSpawnPointsByDistanceSpawning(team, ref spawnPoints);
        Debug.Log("-------------------------------------------------------------");
        if (spawnPoints.Count <= 0)
        {
            Debug.Log("En yakın düşman uzaklıklarına göre uygun spawn noktası bulunamadı.\n");
            Debug.Log("-------------------------------------------------------------");
            GetSpawnPointsBySquadSpawning(team, ref spawnPoints);
            Debug.Log("-------------------------------------------------------------");

        }
        SpawnPoint spawnPoint = spawnPoints.Count <= 1 ? spawnPoints[0] : spawnPoints[_random.Next(0, (int)((float)spawnPoints.Count * .5f))];
        spawnPoint.StartTimer();
               
        return spawnPoint;
    }

    /// <summary>
    /// Düşman oyuncuların uzaklıklarına göre en uygun spawn noktaları hesaplanır.
    /// suitableSpawnPoints listesi nullsa oluşturulur ve temizlenir.
    /// Awakede elde edilen spawn noktaları (_sharedSpawnPoints) en yakın düşman uzaklıklarına göre büyükten küçüğe sıralanır. 
    /// En yakın düşmana olan uzaklık _minDistanceToClosestEnemy değerinden büyük olmak üzere, büyükten küçüğe sıralanmış olan spawn noktalarının arasından 
    ///     -> en yakın arkadaş ve en yakın düşman uzaklıkları _minMemberDistance değerinden büyük ve
    ///     -> Timer'ı aktif olmayanlar (yani 2 sn içerisinde spawn yapılmamış olanlar)
    /// uygun spawn noktaları listesine eklenir. (suitableSpawnPoints)    
    /// 
    /// </summary>
    
    private void GetSpawnPointsByDistanceSpawning(PlayerTeam team, ref List<SpawnPoint> suitableSpawnPoints)
    {
        if (suitableSpawnPoints == null)
        {
            suitableSpawnPoints = new List<SpawnPoint>();
        }
        suitableSpawnPoints.Clear();
        _sharedSpawnPoints.Sort(delegate (SpawnPoint a, SpawnPoint b)
        {
            if (a.DistanceToClosestEnemy == b.DistanceToClosestEnemy)
            {
                return 0;
            }
            if (a.DistanceToClosestEnemy> b.DistanceToClosestEnemy)
            {
                return -1;
            }
            return 1;
        });
        Debug.Log("Spawn noktaları düşman uzaklıklarına göre büyükten küçüğe sıralandı.\n       --Sorted Spawn Points-- ");
        _sharedSpawnPoints.ForEach(t => Debug.Log(t));
        
        for (int i = 0; i < _sharedSpawnPoints.Count && _sharedSpawnPoints[i].DistanceToClosestEnemy > _minDistanceToClosestEnemy; i++)
        {       
            if (!(_sharedSpawnPoints[i].DistanceToClosestFriend <= _minMemberDistance) && !(_sharedSpawnPoints[i].DistanceToClosestEnemy <= _minMemberDistance) && _sharedSpawnPoints[i].SpawnTimer <= 0)
            {
                suitableSpawnPoints.Add(_sharedSpawnPoints[i]);
                
            }
        }
        int control = 0;
        //Spawn Noktalarının Hangisinin Neden Uygun olmadığı bilgisi için
        Debug.Log("NOT SUITABLE");
        for (int i = 0; i < _sharedSpawnPoints.Count;i++)
        {
            
            if (_sharedSpawnPoints[i].DistanceToClosestEnemy <= _minDistanceToClosestEnemy)
            { Debug.Log(_sharedSpawnPoints[i] + "noktasına en yakın düşmana uzaklığı _minDistanceToClosestEnemy= " + _minDistanceToClosestEnemy + "değerinden küçük olduğu için uygun değil"); control++; }
            if((_sharedSpawnPoints[i].DistanceToClosestFriend <= _minMemberDistance) && (_sharedSpawnPoints[i].DistanceToClosestEnemy <= _minMemberDistance))
            {
                Debug.Log(_sharedSpawnPoints[i] + "noktasına en yakın oyuncu uzaklığı _minMemberDistance= " + _minMemberDistance + "değerinden küçük için uygun değil"); control++;
            }
            if (!(_sharedSpawnPoints[i].SpawnTimer <= 0))
            {
                Debug.Log(_sharedSpawnPoints[i] + "noktasının timerı sıfırlanmadı"); control++;
            }
        }
        if (control == 0) { Debug.Log("None"); }
        Debug.Log("SUITABLE");
        suitableSpawnPoints.ForEach(t => Debug.Log(t.ToString()));
        if (suitableSpawnPoints.Count <= 0) { Debug.Log("None"); }
        Debug.Log("En yakın düşman uzaklıklarına göre spawn noktalarının en uygun olanları hesaplandı.\n");
    }

    /// <summary>
    /// Arkadaş oyuncuların uzaklıklarına göre en uygun spawn noktaları hesaplanır.
    /// suitableSpawnPoints listesi nullsa oluşturulur ve temizlenir.
    /// Awakede elde edilen spawn noktaları (_sharedSpawnPoints) en yakın arkadaş uzaklıklarına göre küçükten büyüğe sıralanır. 
    /// En yakın arkadaşa olan uzaklık _maxDistanceToClosestEnemy değerine küçük eşit olmak üzere, küçükten büyüğe sıralanmış olan spawn noktalarının arasından
    ///     -> en yakın arkadaş ve en yakın düşman uzaklıkları _minMemberDistance değerinden büyük ve
    ///     -> Timer'ı aktif olmayanlar (yani 2 sn içerisinde spawn yapılmamış olanlar)
    /// uygun spawn noktaları listesine eklenir. (suitableSpawnPoints)  
    /// Eğer uygun spawn noktası bulunamadıysa _sharedSpawnPoints listesinde küçükten büyüğe sıralanmış noktalardan en küçüğü uygun nokta olarak alınır.
    /// </summary>

    private void GetSpawnPointsBySquadSpawning(PlayerTeam team, ref List<SpawnPoint> suitableSpawnPoints)
    {
        if (suitableSpawnPoints == null)
        {
            suitableSpawnPoints = new List<SpawnPoint>();
        }
        suitableSpawnPoints.Clear();
        _sharedSpawnPoints.Sort(delegate (SpawnPoint a, SpawnPoint b)
        {
            if (a.DistanceToClosestFriend == b.DistanceToClosestFriend)
            {
                return 0;
            }
            if (a.DistanceToClosestFriend > b.DistanceToClosestFriend)
            {
                return 1;
            }
            return -1;
        });
        Debug.Log("Spawn noktaları arkadaş uzaklıklarına göre küçükten büyüğe sıralandı.\n       --Sorted Spawn Points-- ");
        _sharedSpawnPoints.ForEach(t => Debug.Log(t));
        
        for (int i = 0; i < _sharedSpawnPoints.Count && _sharedSpawnPoints[i].DistanceToClosestFriend <= _maxDistanceToClosestFriend; i++)
        {
            
            if (!(_sharedSpawnPoints[i].DistanceToClosestFriend <= _minMemberDistance) && !(_sharedSpawnPoints[i].DistanceToClosestEnemy <= _minMemberDistance) && _sharedSpawnPoints[i].SpawnTimer <= 0) 
            {
                suitableSpawnPoints.Add(_sharedSpawnPoints[i]);
                
            }            
        }

        //Spawn Noktalarının Hangisinin Neden Uygun olmadığı bilgisi için
        int control = 0;
        Debug.Log("NOT SUITABLE");
        for (int i = 0; i < _sharedSpawnPoints.Count; i++)
        {
            if (_sharedSpawnPoints[i].DistanceToClosestEnemy > _maxDistanceToClosestFriend)
            { Debug.Log(_sharedSpawnPoints[i] + "noktasına en yakın arkadaş uzaklığı _maxDistanceToClosestFriend= " + _maxDistanceToClosestFriend + "değerinden büyük olduğu için uygun değil");control++; }
            if ((_sharedSpawnPoints[i].DistanceToClosestFriend <= _minMemberDistance) && (_sharedSpawnPoints[i].DistanceToClosestEnemy <= _minMemberDistance))
            {
                Debug.Log(_sharedSpawnPoints[i] + "noktasına en yakın oyuncu uzaklığı _minMemberDistance= " + _minMemberDistance + "değerinden küçük için uygun değil"); control++;
            }
            if (!(_sharedSpawnPoints[i].SpawnTimer <= 0))
            {
                Debug.Log(_sharedSpawnPoints[i] + "noktasının timerı sıfırlanmadı"); control++;
            }
        }
        if (control == 0) { Debug.Log("None"); }
        Debug.Log(" SUITABLE");
        suitableSpawnPoints.ForEach(t => Debug.Log(t.ToString()));
        if (suitableSpawnPoints.Count <= 0) { Debug.Log("None"); }

        Debug.Log("En yakın arkadaş uzaklıklarına göre spawn noktalarının en uygun olanları hesaplandı.\n");

        if (suitableSpawnPoints.Count <= 0)
        {
            Debug.Log("Uygun nokta bulunamadığından sıralanmış paylaşımlı spawn noktalarından ilki en uygun seçildi.\n");
            suitableSpawnPoints.Add(_sharedSpawnPoints[0]);
        }
        

    }

    /// <summary>
    /// Tüm spawn noktalarının, arkadaş takımdan kendisine en yakın oyuncuya olan uzaklığını GetDistanceToClosestMember metodunda hesaplanan değere atar.
    /// Tüm spawn noktalarının, düşman takımdan kendisine en yakın oyuncuya olan uzaklığını GetDistanceToClosestMember metodunda hesaplanan değere atar.
    /// </summary>

    private void CalculateDistancesForSpawnPoints(PlayerTeam playerTeam)
    {
        for (int i = 0; i < _sharedSpawnPoints.Count; i++)
        {
            _sharedSpawnPoints[i].DistanceToClosestFriend = GetDistanceToClosestMember(_sharedSpawnPoints[i].PointTransform.position, playerTeam);
            _sharedSpawnPoints[i].DistanceToClosestEnemy = GetDistanceToClosestMember(_sharedSpawnPoints[i].PointTransform.position, playerTeam == PlayerTeam.BlueTeam ? PlayerTeam.RedTeam : playerTeam == PlayerTeam.RedTeam ? PlayerTeam.BlueTeam : PlayerTeam.None);
        }
        Debug.Log("Spawn noktalarına en yakın düşman ve en yakın arkadaş oyuncuların uzaklıkları hesaplandı.\n");
    }

  
    /// <summary>
    /// Parametredeki takım değerine ait takımdaki tüm oyuncular arasından koşulları sağlayanların, pozisyon bilgisi verilen spawn noktasına olan uzaklıklarını bulur.
    /// Bu koşullar; oyuncunun disabled olmaması, takım değerinin None olmaması,takım değerinin parametredeki değere eşit olması ve oyuncunun ölü durumda olmamasıdır.
    /// Bu koşulları sağlayan oyuncuların spawn noktasına olan uzaklıkları arasından minimumu seçilir ve döndürülür.
    /// </summary>
    
    private float GetDistanceToClosestMember(Vector3 position, PlayerTeam playerTeam)
    {
        // closestDistance 'a _maxDistanceToClosestFriend değişkeniyle aynı başlangıç değeri verilir.
        _closestDistance = 30;
        foreach (var player in DummyPlayers)
        {
            if (!(player.Disabled) && player.PlayerTeamValue != PlayerTeam.None && player.PlayerTeamValue == playerTeam && !(player.IsDead()))
            {
                float playerDistanceToSpawnPoint = Vector3.Distance(position, player.Transform.position);
                //minimum değerin doğru şekilde bulunabilmesi için if döngüsüne ilk girişte _closestDistance 'ın bir değeri olması gerekir.

                if (playerDistanceToSpawnPoint < _closestDistance)
                 {
                    
                    _closestDistance = playerDistanceToSpawnPoint;
                   
                }
            }
        }

        return _closestDistance;
       
    }

    #endregion
	/// <summary>
	/// Test için paylaşımlı spawn noktalarından en uygun olanını seçer.
	/// Test oyuncusunun pozisyonunu seçilen spawn noktasına atar.
	/// </summary>
    public void TestGetSpawnPoint()
    {
    	SpawnPoint spawnPoint = GetSharedSpawnPoint(PlayerToBeSpawned.PlayerTeamValue);
    	PlayerToBeSpawned.Transform.position = spawnPoint.PointTransform.position;
    }

}