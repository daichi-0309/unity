using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵の攻撃処理を管理するスクリプト
/// 攻撃範囲検出とプレイヤーへのダメージ処理を実装
/// </summary>
public class EnemyCombat : MonoBehaviour
{
    // ========== 攻撃・ダメージ関連 ==========
    [SerializeField] private int damage = 1;              // 1回の攻撃で与えるダメージ

    // ========== 攻撃範囲関連 ==========
    [SerializeField] private Transform attackPoint;       // 攻撃判定の中心位置
    [SerializeField] private float punchRange = 1f;       // 攻撃範囲の半径

    // ========== レイヤー関連 ==========
    [SerializeField] private LayerMask playerLayer;       // プレイヤーのレイヤーマスク

    /// <summary>
    /// 敵の攻撃を実行する
    /// EnemyMovement の Attacking 状態時に、アニメーションイベントから呼び出される
    /// 
    /// 処理：
    /// 1. attackPoint の位置に攻撃範囲を設定
    /// 2. その範囲内のプレイヤーを検出
    /// 3. プレイヤーが見つかった場合、ダメージを与える
    /// </summary>
    public void Attack()
    {
        // attackPoint を中心に punchRange の半径で範囲内のプレイヤーをすべて検出
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, punchRange, playerLayer);

        // プレイヤーが検出された場合
        if (hits.Length > 0)
        {
            // 最初に見つかったプレイヤーに対して処理を実行
            PlayerHealth playerHealth = hits[0].GetComponent<PlayerHealth>();
            
            // PlayerHealth コンポーネントが見つかった場合、ダメージを与える
            if (playerHealth != null)
            {
                // ダメージを与える（敵自身の Transform を攻撃者として渡す）
                playerHealth.ChangeHealth(-damage, transform);
            }
        }
    }
}