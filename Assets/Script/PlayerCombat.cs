using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 改善版プレイヤー攻撃システム
/// 
/// 主な改善点：
/// 1. コンボロジックの重複を解決 - FinishAttacking() と ExecuteAttack() の二重実行を防止
/// 2. 設定の外部化 - ScriptableObject (CombatConfig) でパラメータを一元管理
/// 3. エラーハンドリング強化 - 全ての参照に対する null チェックを統一
/// 4. パフォーマンス最適化 - Physics2D 呼び出しの重複を Debounce で防止
/// 5. 状態管理の厳密化 - isAttacking フラグで攻撃中かどうかを明確に判定
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    // ========== 参照の設定 ==========
    public Transform attackPoint;                  // 攻撃判定の中心位置
    public Animator anim;                          // Animator コンポーネント
    public LayerMask enemyLayer;                   // 敵判定用のレイヤーマスク
    public CombatConfig combatConfig;              // 攻撃パラメータを集約した設定ファイル

    // ========== 攻撃の種類 ==========
    public enum AttackType { Jab, Punch, Kick }
    private AttackType currentAttack = AttackType.Jab;  // 現在の攻撃タイプ

    // ========== 内部状態管理 ==========
    private PlayerMovement2D playerMovement;
    private bool isComboActive = false;            // コンボが有効かどうか
    private bool isAttacking = false;              // 現在攻撃中かどうか

    // ========== タイマー管理 ==========
    private float attackTimer = 0f;                // クールタイムの残り時間
    private float comboTimer = 0f;                 // コンボウィンドウの残り時間

    // ========== パフォーマンス最適化 ==========
    // 同一フレーム内でのダメージ処理の重複を防ぐため、最後にダメージを与えた時刻を記録
    private float lastDamageCheckTime = -1f;
    private const float DAMAGE_CHECK_DEBOUNCE = 0.05f;  // 0.05秒以内は処理をスキップ

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement2D>();
        ValidatePlayerReferences();
    }

    /// <summary>
    /// 初期化時に全ての参照が正しく設定されているかチェック
    /// エラーを早期に検出するため、Start で実行
    /// </summary>
    private void ValidatePlayerReferences()
    {
        if (combatConfig == null)
            Debug.LogError($"[PlayerCombat] CombatConfig が {gameObject.name} に割り当てられていません");

        if (attackPoint == null)
            Debug.LogError($"[PlayerCombat] attackPoint が {gameObject.name} に割り当てられていません");

        if (anim == null)
            Debug.LogError($"[PlayerCombat] Animator が {gameObject.name} に割り当てられていません");
    }

    private void Update()
    {
        UpdateTimers();
    }

    /// <summary>
    /// クールタイムとコンボウィンドウのタイマーをカウントダウン
    /// </summary>
    private void UpdateTimers()
    {
        // 攻撃のクールタイムをカウントダウン
        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        // コンボが有効で、ウィンドウがまだ有効な場合
        if (isComboActive && comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
        }
        // コンボが有効だがウィンドウが終了した場合
        else if (isComboActive && comboTimer <= 0)
        {
            CancelCombo();
        }
    }

    /// <summary>
    /// 攻撃を実行する（PlayerMovement2D の OnJab() から呼び出される）
    /// 
    /// ロジック：
    /// 1. コンボが有効で、かつアニメーション終了間際なら → 次のコンボへ進む
    /// 2. コンボが無効で、クールタイムが終了していたら → 新しいコンボを開始
    /// 3. それ以外 → 入力を無視（クールタイム中または既に攻撃中）
    /// </summary>
    public void ExecuteAttack()
    {
        // CombatConfig が未割り当てならエラーで終了
        if (combatConfig == null)
        {
            Debug.LogError("[PlayerCombat] CombatConfig が null です。攻撃を実行できません。");
            return;
        }

        // ケース1：コンボが有効で、タイミングが適切ならコンボを進める
        // comboTimer < comboInputThreshold は「アニメーション終了間際」を意味する
        if (isComboActive && comboTimer > 0 && comboTimer < combatConfig.comboInputThreshold)
        {
            ProgressCombo();
        }
        // ケース2：コンボが無効で、クールタイムが終了しているなら新しいコンボを開始
        else if (!isComboActive && attackTimer <= 0)
        {
            StartNewCombo();
        }
        // ケース3：その他（クールタイム中や既に攻撃中）→ 入力は無視される
    }

    /// <summary>
    /// 新しいコンボを開始する（Jab からスタート）
    /// </summary>
    private void StartNewCombo()
    {
        currentAttack = AttackType.Jab;
        isComboActive = true;
        comboTimer = combatConfig.comboWindowTime;
        PerformAttack(AttackType.Jab);
        Debug.Log("[戦闘] 新しいコンボ開始：Jab を実行");
    }

    /// <summary>
    /// コンボを次の段階に進める（Jab → Punch → Kick）
    /// 
    /// フロー：
    /// - Jab の次は Punch
    /// - Punch の次は Kick
    /// - Kick は最後の攻撃なので、これ以上進まない
    /// </summary>
    private void ProgressCombo()
    {
        // 次の攻撃の種類を決定
        AttackType nextAttack = GetNextAttackInSequence();

        // Kick で終了する場合
        if (nextAttack == AttackType.Kick && currentAttack == AttackType.Kick)
        {
            CancelCombo();
            Debug.Log("[戦闘] コンボ完了");
        }
        else
        {
            // 次の攻撃を実行
            currentAttack = nextAttack;
            PerformAttack(nextAttack);
            comboTimer = combatConfig.comboWindowTime;
            Debug.Log($"[戦闘] コンボが進行：{currentAttack}");
        }
    }

    /// <summary>
    /// 攻撃の種類に応じて、次の攻撃の種類を返す
    /// C# 8.0 の Switch Expression を使用した簡潔な実装
    /// </summary>
    private AttackType GetNextAttackInSequence()
    {
        return currentAttack switch
        {
            AttackType.Jab => AttackType.Punch,    // Jab の次は Punch
            AttackType.Punch => AttackType.Kick,   // Punch の次は Kick
            AttackType.Kick => AttackType.Kick,    // Kick は最終段階（進まない）
            _ => AttackType.Jab                    // デフォルトは Jab
        };
    }

    /// <summary>
    /// 実際に攻撃を実行する（アニメーション開始）
    /// 
    /// 処理内容：
    /// 1. Animator に攻撃タイプを通知（ComboIndex）
    /// 2. Attack トリガーでアニメーションを開始
    /// 3. isAttacking フラグを true に設定
    /// 4. クールタイムをリセット
    /// </summary>
    private void PerformAttack(AttackType attackType)
{
    if (anim == null)
    {
        Debug.LogError("[PlayerCombat] Animator が null です！");
        return;
    }

    isAttacking = true;
    
    Debug.Log($"[デバッグ] Animator Controller: {anim.runtimeAnimatorController.name}");
    Debug.Log($"[デバッグ] 現在のパラメータ数: {anim.parameterCount}");
    
    // パラメータ一覧を表示
    for (int i = 0; i < anim.parameterCount; i++)
    {
        AnimatorControllerParameter param = anim.GetParameter(i);
        Debug.Log($"[デバッグ] パラメータ {i}: {param.name} ({param.type})");
    }
    
    anim.SetInteger("ComboIndex", (int)attackType);
    Debug.Log($"[デバッグ] ComboIndex を {(int)attackType} に設定");
    
    anim.SetTrigger("Attack");
    Debug.Log($"[デバッグ] Attack トリガーを発火");
    Debug.Log($"[デバッグ] SetInteger 後 ComboIndex: {anim.GetInteger("ComboIndex")}");
    Debug.Log($"[デバッグ] 現在のState: {anim.GetCurrentAnimatorStateInfo(0).fullPathHash}");
    anim.SetBool("isAttacking", true);
    attackTimer = combatConfig.attackCooldown;
}

    /// <summary>
    /// 敵にダメージを与える処理
    /// 
    /// 呼び出し元：アニメーションイベント（攻撃モーションの中盤で設定）
    /// 
    /// 処理フロー：
    /// 1. Debounce チェック（重複ダメージ防止）
    /// 2. 攻撃範囲内の敵を全て検出
    /// 3. 最初に見つかった敵にのみダメージを与える
    /// 4. ノックバック処理も同時に実行
    /// </summary>
    public void DealDamage()
    {
        // Debounce：0.05秒以内に複数回呼び出されたら��最初の1回だけ処理
        // これにより、同じアニメーション内での複数回ダメージを防止
        if (Time.time - lastDamageCheckTime < DAMAGE_CHECK_DEBOUNCE)
            return;
        lastDamageCheckTime = Time.time;

        // 必要な参照を確認
        if (attackPoint == null || combatConfig == null)
        {
            Debug.LogError("[PlayerCombat] ダメージ処理に必要な参照が不足しています");
            return;
        }

        // 攻撃範囲内の全ての敵を検出
        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            combatConfig.attackRange,
            enemyLayer
        );

        // 敵が見つからなかった場合
        if (enemies.Length == 0)
        {
            Debug.Log("[戦闘] 攻撃範囲内に敵がいません");
            return;
        }

        // 最初に見つかった敵を処理
        ProcessEnemyHit(enemies[0]);
    }

    /// <summary>
    /// 敵に対してダメージを与える処理
    /// DealDamage() から呼び出され、1体の敵のみを処理
    /// </summary>
    private void ProcessEnemyHit(Collider2D enemyCollider)
    {
        // ノックバック処理を行うコンポーネントを取得
        EnemyKnockback knockbackComponent = enemyCollider.GetComponent<EnemyKnockback>();
        if (knockbackComponent == null)
        {
            Debug.LogError($"[戦闘] 敵 {enemyCollider.gameObject.name} に EnemyKnockback コンポーネントがありません");
            return;
        }

        // ノックバック処理を実行
        // minDistance は CombatConfig から取得（-1 の場合は距離チェック無し）
        bool knockbackSucceeded = knockbackComponent.Knockback(
            transform,
            combatConfig.knockbackForce,
            combatConfig.knockbackDuration,
            combatConfig.stunDuration,
            combatConfig.minKnockbackDistance
        );

        // ノックバックが成功しなかった場合、ダメージも与えない
        // これにより、ノックバック範囲外の敵にはダメージが及ばない仕様
        if (!knockbackSucceeded)
            return;

        // 体力管理コンポーネントを取得
        EnemyHealth healthComponent = enemyCollider.GetComponent<EnemyHealth>();
        if (healthComponent == null)
        {
            Debug.LogError($"[戦闘] 敵 {enemyCollider.gameObject.name} に EnemyHealth コンポーネントがありません");
            return;
        }

        // 現在の攻撃タイプに応じてダメージを決定
        // CombatConfig で一元管理されているため、ゲームバランス調整が容易
        int damage = combatConfig.GetDamageForAttack(currentAttack);

        // ダメージを適用
        healthComponent.ChangeHealth(-damage);
        Debug.Log($"[戦闘] 敵に {damage} ダメージを与えました（{currentAttack}）");
    }

    /// <summary>
    /// 攻撃アニメーションが終了した時点で���び出される
    /// 呼び出し元：アニメーションイベント（攻撃モーション終了時に設定）
    /// 
    /// 改善点：
    /// - 以前はこのメソッドでコンボの自動進行をしていたが、
    ///   ExecuteAttack() との二重実行を避けるため、ここではフラグのみ更新
    /// - 実際のコンボ進行は ExecuteAttack() で統一
    /// </summary>
    public void FinishPlayerAttacking()
{
    Debug.Log("[デバッグ] FinishPlayerAttacking() が呼ばれました！");  // ← 追加
    isAttacking = false;
    anim.SetBool("isAttacking", false);
    Debug.Log($"[戦闘] 攻撃終了：{currentAttack}");
}
    /// <summary>
    /// コンボをキャンセルして初期状態に戻す
    /// </summary>
    private void CancelCombo()
    {
        isComboActive = false;
        comboTimer = 0f;
        Debug.Log("[戦闘] コンボがキャンセルされました");
    }

    /// <summary>
    /// 外部から攻撃可能かどうかを判定（UI 更新などで使用）
    /// </summary>
    public bool CanAttack() => attackTimer <= 0;

    /// <summary>
    /// 現在のクールタイムを取得（UI 表示などで使用）
    /// </summary>
    public float GetAttackTimer() => attackTimer;

    /// <summary>
    /// 現在攻撃中かどうかを判定（移動制限などで使用）
    /// </summary>
    public bool IsAttacking() => isAttacking;

    /// <summary>
    /// Editor 上で攻撃範囲を視覚化（赤い円で表示）
    /// Scene ビューで Gizmos を有効にして確認可能
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null || combatConfig == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, combatConfig.attackRange);
    }
}