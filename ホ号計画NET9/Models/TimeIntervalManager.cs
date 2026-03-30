using System;
using System.Collections.Generic;
using System.Linq;

namespace ホ号計画.Models
{
    /// <summary>
    /// 時間区間（安静時・タスク時）の管理クラス
    /// Excelでの区間設定機能を実現
    /// </summary>
    public class TimeIntervalManager
    {
        #region プロパティ
        
        /// <summary>安静時区間</summary>
        public TimeInterval RestInterval { get; set; }
        
        /// <summary>タスク区間リスト</summary>
        public List<TimeInterval> TaskIntervals { get; private set; }
        
        /// <summary>データの総時間</summary>
        public double TotalDuration { get; set; }
        
        /// <summary>区間が設定済みかどうか</summary>
        public bool IsConfigured => RestInterval != null && TaskIntervals.Count > 0;
        
        #endregion

        #region コンストラクタ
        
        public TimeIntervalManager()
        {
            TaskIntervals = new List<TimeInterval>();
        }
        
        public TimeIntervalManager(double totalDuration) : this()
        {
            TotalDuration = totalDuration;
        }
        
        #endregion

        #region 区間設定メソッド
        
        /// <summary>
        /// 安静時区間を設定
        /// </summary>
        /// <param name="startTime">開始時刻（秒）</param>
        /// <param name="endTime">終了時刻（秒）</param>
        /// <param name="label">区間ラベル（デフォルト: "安静時"）</param>
        public void SetRestInterval(double startTime, double endTime, string label = "安静時")
        {
            ValidateTimeRange(startTime, endTime);
            
            RestInterval = new TimeInterval
            {
                StartTime = startTime,
                EndTime = endTime,
                Label = label,
                IntervalType = IntervalType.Rest
            };
        }
        
        /// <summary>
        /// タスク区間を追加
        /// </summary>
        /// <param name="startTime">開始時刻（秒）</param>
        /// <param name="endTime">終了時刻（秒）</param>
        /// <param name="label">区間ラベル</param>
        public void AddTaskInterval(double startTime, double endTime, string label)
        {
            ValidateTimeRange(startTime, endTime);
            ValidateNonOverlappingInterval(startTime, endTime);
            
            var taskInterval = new TimeInterval
            {
                StartTime = startTime,
                EndTime = endTime,
                Label = label,
                IntervalType = IntervalType.Task
            };
            
            TaskIntervals.Add(taskInterval);
            
            // 時間順にソート
            TaskIntervals = TaskIntervals.OrderBy(t => t.StartTime).ToList();
        }
        
        /// <summary>
        /// 2分間隔でタスク区間を自動生成
        /// </summary>
        /// <param name="taskStartTime">最初のタスク開始時刻</param>
        /// <param name="taskDuration">各タスクの時間（秒、デフォルト120秒=2分）</param>
        /// <param name="taskCount">タスク数</param>
        /// <param name="intervalBetweenTasks">タスク間の間隔（秒、デフォルト0秒=連続）</param>
        public void AddTaskIntervalsSeries(double taskStartTime, double taskDuration = 120.0, 
                                          int taskCount = 5, double intervalBetweenTasks = 0.0)
        {
            for (int i = 0; i < taskCount; i++)
            {
                double start = taskStartTime + i * (taskDuration + intervalBetweenTasks);
                double end = start + taskDuration;
                
                // データ範囲チェック
                if (end > TotalDuration)
                {
                    Console.WriteLine($"警告: タスク{i + 1}が総時間を超過するため追加を停止します");
                    break;
                }
                
                string label = $"タスク{i + 1}";
                AddTaskInterval(start, end, label);
            }
        }
        
        /// <summary>
        /// 特定のタスク区間を削除
        /// </summary>
        /// <param name="label">削除する区間のラベル</param>
        /// <returns>削除成功の場合true</returns>
        public bool RemoveTaskInterval(string label)
        {
            var intervalToRemove = TaskIntervals.FirstOrDefault(t => t.Label == label);
            if (intervalToRemove != null)
            {
                TaskIntervals.Remove(intervalToRemove);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 全タスク区間をクリア
        /// </summary>
        public void ClearTaskIntervals()
        {
            TaskIntervals.Clear();
        }
        
        #endregion

        #region 検索・取得メソッド
        
        /// <summary>
        /// 指定時刻がどの区間に属するかを判定
        /// </summary>
        /// <param name="time">時刻（秒）</param>
        /// <returns>該当する区間、見つからない場合はnull</returns>
        public TimeInterval GetIntervalAtTime(double time)
        {
            // 安静時区間チェック
            if (RestInterval != null && time >= RestInterval.StartTime && time <= RestInterval.EndTime)
            {
                return RestInterval;
            }
            
            // タスク区間チェック
            return TaskIntervals.FirstOrDefault(t => time >= t.StartTime && time <= t.EndTime);
        }
        
        /// <summary>
        /// 全区間を時間順に取得
        /// </summary>
        public List<TimeInterval> GetAllIntervals()
        {
            var allIntervals = new List<TimeInterval>();
            
            if (RestInterval != null)
                allIntervals.Add(RestInterval);
            
            allIntervals.AddRange(TaskIntervals);
            
            return allIntervals.OrderBy(t => t.StartTime).ToList();
        }
        
        /// <summary>
        /// 時間軸配列から指定区間のインデックスを取得
        /// </summary>
        /// <param name="interval">対象区間</param>
        /// <param name="timeAxis">時間軸配列</param>
        /// <returns>区間に含まれるインデックスのリスト</returns>
        public List<int> GetTimeIndicesForInterval(TimeInterval interval, double[] timeAxis)
        {
            var indices = new List<int>();
            
            for (int i = 0; i < timeAxis.Length; i++)
            {
                if (timeAxis[i] >= interval.StartTime && timeAxis[i] <= interval.EndTime)
                {
                    indices.Add(i);
                }
            }
            
            return indices;
        }
        
        #endregion

        #region 検証メソッド
        
        /// <summary>
        /// 区間設定の整合性を検証
        /// </summary>
        public bool ValidateIntervals()
        {
            try
            {
                // 安静時区間の基本チェック
                if (RestInterval == null)
                    return false;
                
                ValidateTimeRange(RestInterval.StartTime, RestInterval.EndTime);
                
                // タスク区間の基本チェック
                foreach (var task in TaskIntervals)
                {
                    ValidateTimeRange(task.StartTime, task.EndTime);
                }
                
                // 重複チェック
                ValidateNonOverlappingIntervals();
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 時間範囲の妥当性チェック
        /// </summary>
        private void ValidateTimeRange(double startTime, double endTime)
        {
            if (startTime < 0)
                throw new ArgumentException("開始時刻は0以上である必要があります");
            
            if (endTime <= startTime)
                throw new ArgumentException("終了時刻は開始時刻より大きい必要があります");
            
            if (TotalDuration > 0 && endTime > TotalDuration)
                throw new ArgumentException($"終了時刻が総時間（{TotalDuration}秒）を超過しています");
        }
        
        /// <summary>
        /// 新しい区間が既存区間と重複しないかチェック
        /// </summary>
        private void ValidateNonOverlappingInterval(double startTime, double endTime)
        {
            // 安静時区間との重複チェック
            if (RestInterval != null && 
                !(endTime <= RestInterval.StartTime || startTime >= RestInterval.EndTime))
            {
                throw new ArgumentException("安静時区間と重複しています");
            }
            
            // 他のタスク区間との重複チェック
            foreach (var existingTask in TaskIntervals)
            {
                if (!(endTime <= existingTask.StartTime || startTime >= existingTask.EndTime))
                {
                    throw new ArgumentException($"既存のタスク区間（{existingTask.Label}）と重複しています");
                }
            }
        }
        
        /// <summary>
        /// 全区間の重複チェック
        /// </summary>
        private void ValidateNonOverlappingIntervals()
        {
            var allIntervals = GetAllIntervals();
            
            for (int i = 0; i < allIntervals.Count; i++)
            {
                for (int j = i + 1; j < allIntervals.Count; j++)
                {
                    var interval1 = allIntervals[i];
                    var interval2 = allIntervals[j];
                    
                    if (!(interval1.EndTime <= interval2.StartTime || 
                          interval1.StartTime >= interval2.EndTime))
                    {
                        throw new ArgumentException($"区間 '{interval1.Label}' と '{interval2.Label}' が重複しています");
                    }
                }
            }
        }
        
        #endregion

        #region ユーティリティメソッド
        
        /// <summary>
        /// 区間設定の要約を取得
        /// </summary>
        public string GetSummary()
        {
            var summary = "=== 時間区間設定 ===\n";
            
            if (RestInterval != null)
            {
                summary += $"安静時: {RestInterval.StartTime:F1} - {RestInterval.EndTime:F1}秒 " +
                          $"（{RestInterval.Duration:F1}秒間）\n";
            }
            else
            {
                summary += "安静時: 未設定\n";
            }
            
            if (TaskIntervals.Count > 0)
            {
                summary += $"タスク区間: {TaskIntervals.Count}個\n";
                foreach (var task in TaskIntervals)
                {
                    summary += $"  {task.Label}: {task.StartTime:F1} - {task.EndTime:F1}秒 " +
                              $"（{task.Duration:F1}秒間）\n";
                }
            }
            else
            {
                summary += "タスク区間: 未設定\n";
            }
            
            summary += $"総時間: {TotalDuration:F1}秒\n";
            summary += $"設定状態: {(IsConfigured ? "設定済み" : "未完了")}\n";
            summary += $"検証結果: {(ValidateIntervals() ? "OK" : "NG")}\n";
            
            return summary;
        }
        
        /// <summary>
        /// デフォルト区間設定を作成（テスト用）
        /// </summary>
        /// <param name="totalDuration">総時間</param>
        public void SetDefaultIntervals(double totalDuration)
        {
            TotalDuration = totalDuration;
            
            // 最初の2分を安静時とする
            SetRestInterval(0, 120, "安静時");
            
            // 2分後から2分間隔で5つのタスク区間を設定
            AddTaskIntervalsSeries(120, 120, 5);
        }
        
        #endregion
    }

    #region 区間タイプ列挙型
    
    /// <summary>
    /// 時間区間のタイプ
    /// </summary>
    public enum IntervalType
    {
        /// <summary>安静時</summary>
        Rest,
        /// <summary>タスク時</summary>
        Task,
        /// <summary>その他</summary>
        Other
    }
    
    #endregion
}