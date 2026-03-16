using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵の体力を管理するスクリプト
/// ダメージ受付と死亡処理を実装
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    // ========== 体力関連 ==========
    [SerializeField] private int currentHealth;        // 現在の体力
    [SerializeField] private int maxHealth = 10;       // 最大体力（デフォルト：10）

    private void Start()
    {
        // ゲーム開始時に体力を最大値で初期化
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 敵の体力を変更する
    /// </summary>
    /// <param name="amount">変更量（負の値でダメージ、正の値で回復）</param>
    public void ChangeHealth(int amount)
    {
        // 体力を更新
        currentHealth += amount;

        // 体力が最大値を超えないようにクランプ
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        // 体力が 0 以下になったら敵を削除
        else if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }
}