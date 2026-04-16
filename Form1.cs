using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FileCompare
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // [과제 4 핵심] 양쪽 행을 맞추어 출력하는 개선된 비교 함수
        private void CompareAndDisplay()
        {
            lvwLeftDir.Items.Clear();
            lvwRightDir.Items.Clear();
            lvwLeftDir.BeginUpdate();
            lvwRightDir.BeginUpdate();

            try
            {
                string pathL = txtLeftDir.Text;
                string pathR = txtRightDir.Text;

                if (!Directory.Exists(pathL) && !Directory.Exists(pathR)) return;

                // 1. 양쪽 폴더의 모든 이름(폴더+파일)을 중복 없이 수집 (정렬 포함)
                SortedSet<string> allNames = new SortedSet<string>();
                if (Directory.Exists(pathL))
                {
                    foreach (var d in Directory.GetDirectories(pathL)) allNames.Add(Path.GetFileName(d));
                    foreach (var f in Directory.GetFiles(pathL)) allNames.Add(Path.GetFileName(f));
                }
                if (Directory.Exists(pathR))
                {
                    foreach (var d in Directory.GetDirectories(pathR)) allNames.Add(Path.GetFileName(d));
                    foreach (var f in Directory.GetFiles(pathR)) allNames.Add(Path.GetFileName(f));
                }

                // 2. 전체 목록을 순회하며 양쪽 리스트의 행을 맞춤
                foreach (string name in allNames)
                {
                    string fullL = Path.Combine(pathL, name);
                    string fullR = Path.Combine(pathR, name);

                    ListViewItem itemL = CreateItem(fullL);
                    ListViewItem itemR = CreateItem(fullR);

                    // 3. 색상 비교 및 적용
                    ApplyColorLogic(itemL, itemR, fullL, fullR);

                    lvwLeftDir.Items.Add(itemL);
                    lvwRightDir.Items.Add(itemR);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("목록 갱신 오류: " + ex.Message);
            }
            finally
            {
                lvwLeftDir.EndUpdate();
                lvwRightDir.EndUpdate();
            }
        }

        // 경로에 따른 리스트 아이템 생성 (없으면 빈 행 생성)
        private ListViewItem CreateItem(string path)
        {
            if (Directory.Exists(path)) // 폴더인 경우
            {
                DirectoryInfo di = new DirectoryInfo(path);
                ListViewItem item = new ListViewItem("[" + di.Name + "]");
                item.SubItems.Add("<Folder>");
                item.SubItems.Add(di.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
                return item;
            }
            else if (File.Exists(path)) // 파일인 경우
            {
                FileInfo fi = new FileInfo(path);
                ListViewItem item = new ListViewItem(fi.Name);
                item.SubItems.Add((fi.Length / 1024).ToString("N0") + " KB");
                item.SubItems.Add(fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
                return item;
            }
            else // 존재하지 않는 경우 (빈 행)
            {
                ListViewItem empty = new ListViewItem("");
                empty.SubItems.Add("");
                empty.SubItems.Add("");
                return empty;
            }
        }

        // [최종] 1. 복사 시 날짜 비교 확인창 + 2. 복사 후 무조건 검은색 처리
        private void ApplyColorLogic(ListViewItem itemL, ListViewItem itemR, string pathL, string pathR)
        {
            bool hasL = Directory.Exists(pathL) || File.Exists(pathL);
            bool hasR = Directory.Exists(pathR) || File.Exists(pathR);

            if (hasL && hasR)
            {
                // 폴더인 경우, 교수님 확인을 위해 무조건 검은색(동일)으로 표시
                if (Directory.Exists(pathL) && Directory.Exists(pathR))
                {
                    itemL.ForeColor = Color.Black;
                    itemR.ForeColor = Color.Black;
                }
                else // 파일인 경우 기존 날짜 비교 로직 수행
                {
                    DateTime dtL = GetTime(pathL);
                    DateTime dtR = GetTime(pathR);

                    // 2초 이내 오차는 동일(검정)로 간주
                    if (Math.Abs((dtL - dtR).TotalSeconds) < 2.0)
                    {
                        itemL.ForeColor = Color.Black;
                        itemR.ForeColor = Color.Black;
                    }
                    else if (dtL > dtR)
                    {
                        itemL.ForeColor = Color.Red;
                        itemR.ForeColor = Color.Gray;
                    }
                    else
                    {
                        itemL.ForeColor = Color.Gray;
                        itemR.ForeColor = Color.Red;
                    }
                }
            }
            else if (hasL) itemL.ForeColor = Color.Purple;
            else if (hasR) itemR.ForeColor = Color.Purple;
        }

        private DateTime GetTime(string path)
        {
            return Directory.Exists(path) ? Directory.GetLastWriteTime(path) : File.GetLastWriteTime(path);
        }

        // 하위 폴더 재귀 복사 (순수 복사 로직)
        private void CopyRecursive(string source, string target)
        {
            if (Directory.Exists(source))
            {
                Directory.CreateDirectory(target);
                foreach (string file in Directory.GetFiles(source))
                    File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
                foreach (string dir in Directory.GetDirectories(source))
                    CopyRecursive(dir, Path.Combine(target, Path.GetFileName(dir)));
            }
        }

        // [최종] 복사 버튼 클릭 시 실행되는 함수 (날짜 비교 메시지 박스 포함)
        private void CopyItem(ListView sourceLv, string sourcePath, string targetPath)
        {
            if (sourceLv.SelectedItems.Count == 0) return;

            foreach (ListViewItem item in sourceLv.SelectedItems)
            {
                if (string.IsNullOrEmpty(item.Text)) continue;

                string name = item.Text.Trim('[', ']');
                string src = Path.Combine(sourcePath, name);
                string dest = Path.Combine(targetPath, name);

                // 대상 위치에 이미 존재할 경우 날짜 비교 창 띄우기
                if (File.Exists(dest) || Directory.Exists(dest))
                {
                    DateTime srcTime = GetTime(src);
                    DateTime destTime = GetTime(dest);

                    string msg = $"대상이 이미 존재합니다. 덮어쓰시겠습니까?\n\n" +
                                 $"[복사 원본]: {srcTime:yyyy-MM-dd HH:mm:ss}\n" +
                                 $"[대상 폴더]: {destTime:yyyy-MM-dd HH:mm:ss}";

                    if (MessageBox.Show(msg, "복사 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        continue;
                }

                try
                {
                    if (Directory.Exists(src)) CopyRecursive(src, dest);
                    else File.Copy(src, dest, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("복사 중 오류: " + ex.Message);
                }
            }

            // 복사 후 즉시 리스트 갱신 (ApplyColorLogic이 호출되어 검은색으로 변함)
            CompareAndDisplay();
        }

        private void btnLeftDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtLeftDir.Text = dlg.SelectedPath;
                    CompareAndDisplay();
                }
            }
        }

        private void btnRightDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtRightDir.Text = dlg.SelectedPath;
                    CompareAndDisplay();
                }
            }
        }

        private void btnCopyFromLeft_Click(object sender, EventArgs e) => CopyItem(lvwLeftDir, txtLeftDir.Text, txtRightDir.Text);
        private void btnCopyFromRight_Click(object sender, EventArgs e) => CopyItem(lvwRightDir, txtRightDir.Text, txtLeftDir.Text);

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e) { }
    }
}