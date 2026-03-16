using UnityEngine;

/// <summary>
/// 改善版：敵の移動・状態管理
/// 
/// 主な改善点：
/// 1. CombatConfig を使用して設定を一元化
///    - 敵の検出範囲、攻撃範囲、クールタイムなど
///    - 複数の敵で同じ設定を共有可能
/// 
/// 2. 状態遷移をより明確に
///    - ChangeState() メソッドで状態管理を統一
/// 
/// 3. 検出ロジックをシンプル化
///    - 早期 return を使用した Guard Clause パターン
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    // ========== 設定参照 ==========
    [Header("設定")]
    [Tooltip("戦闘システムの設定ファイル（複数の敵で共有）")]
    public CombatConfig combatConfig;

    [SerializeField] private Transform detectionPoint;  // 敵検出の中心位置
    [SerializeField] private LayerMask playerLayer;     // プレイヤーのレイヤーマスク

    // ========== 敵の状態管理 ==========
    private EnemyState enemyState = EnemyState.Idle;    // 敵の現在の状態
    private int facingDirection = -1;                   // 敵の向き（-1 = 左、1 = 右）

    // ========== コンポーネント参照 ==========
    private Rigidbody2D rb;
    private Animator anim;

    // ========== 追跡関連 ==========
    private Transform player;                           // プレイヤーの Transform
    private float attackCooldownTimer = 0f;             // 次の攻撃までの時間

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // 設定ファイルの確認
        if (combatConfig == null)
            Debug.LogError($"[EnemyMovement] {gameObject.name} に CombatConfig が割り当てられていません");

        // 初期状態を Idle に設定
        ChangeState(EnemyState.Idle);
    }

    private void Update()
    {
        // ========== ノックバック中は他の処理をスキップ ==========
        // ノックバック処理は EnemyKnockback が管理するため、ここでは処理しない
        if (enemyState == EnemyState.Knockback)
            return;

        // 毎フレーム実行する処理
        CheckForPlayer();          // プレイヤーを検出
        UpdateAttackCooldown();    // 攻撃クールタイムをカウントダウン
        UpdateState();             // 現在の状態に応じた処理を実行
    }

    /// <summary>
    /// プレイヤーを検出し、距離に応じて敵の状態を更新
    /// </summary>
    private void CheckForPlayer()
    {
        // CombatConfig が未割り当てなら処理中止
        if (combatConfig == null || detectionPoint == null)
            return;

        // ========== プレイヤーを検出 ==========
        // detectionPoint を中心に、半径 enemyDetectRange の範囲内でプレイヤーを検出
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            detectionPoint.position,
            combatConfig.enemyDetectRange,
            playerLayer
        );

        // ========== プレイヤーが見つかった場合 ==========
        if (hits.Length > 0)
        {
            player = hits[0].transform;
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // ケース1：攻��範囲内 AND 攻撃クールタイムが終了
            if (distanceToPlayer <= combatConfig.enemyAttackRange && attackCooldownTimer <= 0)
            {
                attackCooldownTimer = combatConfig.enemyAttackCooldown;
                ChangeState(EnemyState.Attacking);
            }
            // ケース2：攻撃範囲外 → 追跡状態へ
            else if (distanceToPlayer > combatConfig.enemyAttackRange)
            {
                ChangeState(EnemyState.Chasing);
            }
        }
        // ========== プレイヤーが見つからない場合 ==========
        else
        {
            rb.linearVelocity = Vector2.zero;
            ChangeState(EnemyState.Idle);
            player = null;
        }
    }

    /// <summary>
    /// 攻撃クールタイムをカウントダウン
    /// </summary>
    private void UpdateAttackCooldown()
    {
        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;
    }

    /// <summary>
    /// 現在の状態に応じた処理を実行
    /// </summary>
    private void UpdateState()
    {
        // 追跡状態かつプレイヤーが存在する場合 → プレイヤーを追跡
        if (enemyState == EnemyState.Chasing && player != null)
        {
            Chase();
        }
        // 攻撃状態 → 移動を停止
        else if (enemyState == EnemyState.Attacking)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// プレイヤーを追跡する
    /// </summary>
    private void Chase()
    {
        if (player == null) return;

        // ========== 敵の向きを更新 ==========
        // プレイヤーが敵の右側にいるなら右を向く、左側なら左を向く
        if ((player.position.x > transform.position.x && facingDirection == -1)
            || (player.position.x < transform.position.x && facingDirection == 1))
        {
            Flip();
        }

        // ========== 移動ベクトルを計算 ==========
        // プレイヤーに向かう方向を計算
        Vector2 direction = (player.position - transform.position).normalized;

        // ========== Rigidbody2D に速度を設定 ==========
        // CombatConfig の enemyDetectRange を速度として使用
        // （本来は enemySpeed などの専用パラメータを設定すべき）
        rb.linearVelocity = direction * combatConfig.enemyDetectRange;
    }

    /// <summary>
    /// 敵の向きを左右反転する
    /// </summary>
    private void Flip()
    {
        facingDirection *= -1;
        transform.localScale = new Vector3(
            transform.localScale.x * -1,
            transform.localScale.y,
            transform.localScale.z
        );
    }

    /// <summary>
    /// 敵の状態を変更し、対応するアニメーションをトリガー
    /// 
    /// フロー：
    /// 1. 前の状態のアニメーション bool をすべて false に
    /// 2. 新しい状態に更新
    /// 3. 新しい状態に対応するアニメーション bool を true に
    /// </summary>
    public void ChangeState(EnemyState newState)
    {
        // ========== 前の状態をリセット ==========
        anim.SetBool("isIdle", false);
        anim.SetBool("isChasing", false);
        anim.SetBool("isAttacking", false);

        // ========== 状態を更新 ==========
        enemyState = newState;

        // ========== 新しい状態のアニメーションを設定 ==========
        switch (enemyState)
        {
            case EnemyState.Idle:
                anim.SetBool("isIdle", true);
                break;
            case EnemyState.Chasing:
                anim.SetBool("isChasing", true);
                break;
            case EnemyState.Attacking:
                anim.SetBool("isAttacking", true);
                break;
            case EnemyState.Knockback:
                // ノックバック状態ではアニメーション bool を設定しない
                // EnemyKnockback が状態管理を行う
                break;
        }
    }

    /// <summary>
    /// Editor 上で敵の検出範囲を視覚化
    /// Scene ビューで Gizmos を有効にして確認可能
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (detectionPoint == null || combatConfig == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(detectionPoint.position, combatConfig.enemyDetectRange);
    }
}