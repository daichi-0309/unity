using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 改善版：プレイヤーの2D移動制御
/// 
/// 主な改善点：
/// 1. 参照のバリデーション - Start で全ての参照を確認
/// 2. 入力処理の簡潔化 - null チェックを統一
/// 3. 状態管理の明確化 - ノックバック・攻撃状態を適切に判定
/// 4. Guard Clause の使用 - 早期 return で可読性向上
/// </summary>
public class PlayerMovement2D : MonoBehaviour
{
    // ========== 移動関連の設定 ==========
    [Header("移動設定")]
    [Tooltip("プレイヤーの移動速度")]
    public float moveSpeed = 5f;

    [Tooltip("Rigidbody2D コンポーネント")]
    public Rigidbody2D rb;

    [Tooltip("Animator コンポーネント")]
    public Animator anim;

    // ========== 戦闘関連の参照 ==========
    [Header("戦闘関連")]
    [Tooltip("プレイヤーの攻撃処理を行うコンポーネント")]
    public PlayerCombat playerCombat;

    // ========== 内部状態管理 ==========
    private Vector2 movement = Vector2.zero;        // 現在の入力方向
    private bool isFacingRight = true;              // プレイヤーが右を向いているか
    private PlayerHealth playerHealth;              // プレイヤーの体力・状態管理

    private void Start()
    {
        // PlayerHealth コンポーネントを同じ GameObject から取得
        playerHealth = GetComponent<PlayerHealth>();
        
        // 全ての参照をバリデーション
        ValidatePlayerReferences();
    }

    /// <summary>
    /// 初期化時に全ての参照が正しく設定されているかチェック
    /// 実行時エラーを早期に検出するため、Start で実行
    /// </summary>
    private void ValidatePlayerReferences()
    {
        if (rb == null)
            Debug.LogError($"[PlayerMovement] {gameObject.name} に Rigidbody2D が割り当てられていません");

        if (anim == null)
            Debug.LogError($"[PlayerMovement] {gameObject.name} に Animator が割り当てられていません");

        if (playerCombat == null)
            Debug.LogError($"[PlayerMovement] {gameObject.name} に PlayerCombat が割り当てられていません");

        if (playerHealth == null)
            Debug.LogError($"[PlayerMovement] {gameObject.name} に PlayerHealth が割り当てられていません");
    }

    /// <summary>
    /// Input System から移動入力を受け取る
    /// 呼び出し元：Input System の "Move" アクション
    /// 
    /// 処理内容：
    /// 1. playerCombat が存在するか確認（Guard Clause）
    /// 2. 攻撃中は移動入力を無視
    /// 3. 攻撃中でなければ入力を受け付ける
    /// 4. プレイヤーの向きを入力方向に合わせる
    /// 5. Animator に移動速度をセット
    /// </summary>
    public void OnMove(InputValue value)
    {
        // ========== Guard Clause：playerCombat または Animator が未割り当て ==========
        if (playerCombat?.anim == null)
            return;

        // ========== 攻撃中は移動入力を無視 ==========
        // isAttacking フラグが true の場合、移動を受け付けない
        if (playerCombat.anim.GetBool("isAttacking"))
        {
            movement = Vector2.zero;
            anim.SetFloat("Speed", 0);
            return;
        }

        // ========== 移動入力を取得 ==========
        // Input System から Vector2 を取得
        movement = value.Get<Vector2>();

        // ========== Animator に移動速度をセット ==========
        // sqrMagnitude を使用（magnitude より計算が高速）
        // 0 = 停止、1 = 移動中
        anim.SetFloat("Speed", movement.sqrMagnitude);

        // ========== プレイヤーの向きを更新 ==========
        // 右への入力で右を向いていない場合 → 反転
        if (movement.x > 0 && !isFacingRight)
        {
            Flip();
        }
        // 左への入力で右を向いている場合 → 反転
        else if (movement.x < 0 && isFacingRight)
        {
            Flip();
        }
    }

    /// <summary>
    /// Input System から攻撃入力を受け取る
    /// 呼び出し元：Input System の "Jab" アクション（押されたときのみ）
    /// 
    /// 処理内容：
    /// 1. 入力が押された状態か確認
    /// 2. playerCombat が存在するか確認
    /// 3. ExecuteAttack() を呼び出し、攻撃処理に委譲
    /// 
    /// 重要：
    /// - Input System のイベント駆動により、自動的に1フレーム1回だけ呼び出される
    /// - 複数回連続で呼び出されることはない（自動的に制御される）
    /// </summary>
    public void OnJab(InputValue value)
    {
        // ========== Guard Clause：入力が押されていない、または playerCombat が未割り当て ==========
        if (!value.isPressed || playerCombat == null)
            return;

        // ========== 攻撃処理を実行 ==========
        // 実際の攻撃ロジックは PlayerCombat に委譲
        Debug.Log("[入力] 攻撃入力が検出されました");
        playerCombat.ExecuteAttack();
    }

    /// <summary>
    /// プレイヤーの向きを左右反転させる
    /// 
    /// 処理：
    /// 1. isFacingRight フラグを反転
    /// 2. localScale.x を -1 倍にして視覚的に反転
    /// 
    /// 注意：
    /// - localScale は子オブジェクトにも影響するため、
    ///   重要な子オブジェクトがある場合は注意が必要
    /// </summary>
    void Flip()
    {
        // 向きを反転
        isFacingRight = !isFacingRight;

        // スケールを反転（視覚的に左右反転）
        Vector3 currentScale = transform.localScale;
        currentScale.x *= -1;
        transform.localScale = currentScale;

        // 向きを文字列で取得
        string direction = isFacingRight ? "右" : "左";
        Debug.Log($"[移動] プレイヤーが {direction} を向きました");
    }

    /// <summary>
    /// 物理演算フレームで実行される処理（移動の適用）
    /// 
    /// フロー：
    /// 1. ノックバック状態か確認 → yes なら移動を停止
    /// 2. 攻撃中か確認 → yes なら移動を停止
    /// 3. そのほか → movement ベクトルに基づいて移動
    /// 
    /// なぜ FixedUpdate か？
    /// - Rigidbody2D の移動は物理エンジン側の時間刻みで行われる
    /// - FixedUpdate は物理エンジンと同期されているため、安定した移動が可能
    /// </summary>
    void FixedUpdate()
    {
        // ========== Guard Clause：必要な参照が存在するか確認 ==========
        if (rb == null || playerHealth == null || playerCombat == null)
        {
            return;
        }

        // ========== ケース1：ノックバック中 ==========
        // playerHealth.IsKnockedBack() が true の場合、移動を一切受け付けない
        if (playerHealth.IsKnockedBack())
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // ========== ケース2：攻撃中 ==========
        // 攻撃中は移動を停止（アニメーション重視）
        if (playerCombat.anim.GetBool("isAttacking"))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // ========== ケース3：通常移動 ==========
        // movement ベクトルに基づいて移動を適用
        // MovePosition を使用（Rigidbody2D の推奨方法）
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}