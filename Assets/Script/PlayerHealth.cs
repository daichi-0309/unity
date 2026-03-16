using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤーの体力管理とノックバック処理を行うスクリプト
/// ダメージ受付、UI更新、ノックバック機能を実装
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    // ========== 体力関連 ==========
    public int currentHealth;                      // 現在の体力
    public int maxHealth;                          // 最大体力
    public Slider slider;                          // HPバーのUI（Slider）

    // ========== ノックバック関連 ==========
    public float knockbackForce = 10f;             // ノックバック時に加える力の大きさ
    public float knockbackDuration = 0.2f;         // ノックバック状態の継続時間（秒）
    private float knockbackTimer = 0f;             // ノックバック状態の残り時間カウント
    private Rigidbody2D rb;                        // Rigidbody2D コンポーネント
    private bool isKnockedBack = false;            // 現在ノックバック中かどうか

    void Start()
    {
        // Rigidbody2D を取得
        rb = GetComponent<Rigidbody2D>();
        
        // HPバーの最大値を設定
        slider.maxValue = maxHealth;
        // HPバーを現在の体力に同期
        slider.value = currentHealth;
    }

    void Update()
    {
        // ノックバック時間をカウントダウン
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            
            // ノックバック時間が終了したら状態をリセット
            if (knockbackTimer <= 0)
            {
                isKnockedBack = false;
                rb.linearVelocity = Vector2.zero;  // 速度をリセット（ノックバック終了）
            }
        }
    }

    /// <summary>
    /// 体力を変更する
    /// amount: 変更量（負の値でダメージ、正の値で回復）
    /// attacker: 攻撃者の Transform（ノックバック方向の計算に使用）
    /// </summary>
    public void ChangeHealth(int amount, Transform attacker = null)
    {
        // 体力を更新
        currentHealth += amount;
        // HPバーを更新
        slider.value = currentHealth;

        // ダメージを受けた場合かつ攻撃者の情報がある場合、ノックバック処理を実行
        if (amount < 0 && attacker != null)
        {
            ApplyKnockback(attacker);
        }

        // 体力が 0 以下になった場合、死亡処理
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            gameObject.SetActive(false);  // ゲームオブジェクトを非アクティブ化
        }
    }

    /// <summary>
    /// ノックバック処理を適用する
    /// 攻撃者の位置からプレイヤーが飛ぶ方向を計算し、Rigidbody2D に速度を与える
    /// </summary>
    private void ApplyKnockback(Transform attacker)
    {
        isKnockedBack = true;
        knockbackTimer = knockbackDuration;

        // 攻撃者からの方向を計算（プレイヤーが敵から離れる方向）
        Vector2 knockbackDirection = (transform.position - attacker.position).normalized;

        // ノックバックを Rigidbody2D に適用
        rb.linearVelocity = knockbackDirection * knockbackForce;
    }

    /// <summary>
    /// 外部からノックバック状態を確認
    /// PlayerMovement2D などで移動制限に使用
    /// </summary>
    public bool IsKnockedBack()
    {
        return isKnockedBack;
    }
}