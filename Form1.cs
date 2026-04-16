using System;
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

        // [과제 2 수정] 한 쪽만 선택해도 목록이 나오며, 양쪽 선택 시 비교 색상 적용
        private void CompareAndDisplay()
        {
            // 리스트뷰 초기화 및 업데이트 성능 최적화
            lvwLeftDir.Items.Clear();
            lvwRightDir.Items.Clear();
            lvwLeftDir.BeginUpdate();
            lvwRightDir.BeginUpdate();

            try
            {
                // 1. 왼쪽 폴더 처리 (경로가 있을 때만)
                if (Directory.Exists(txtLeftDir.Text))
                {
                    DirectoryInfo leftDir = new DirectoryInfo(txtLeftDir.Text);
                    foreach (FileInfo file in leftDir.GetFiles())
                    {
                        // 상대방(오른쪽) 경로를 넘겨줌 (비교 대상)
                        AddFileToListView(lvwLeftDir, file, txtRightDir.Text);
                    }
                }

                // 2. 오른쪽 폴더 처리 (경로가 있을 때만)
                if (Directory.Exists(txtRightDir.Text))
                {
                    DirectoryInfo rightDir = new DirectoryInfo(txtRightDir.Text);
                    foreach (FileInfo file in rightDir.GetFiles())
                    {
                        // 상대방(왼쪽) 경로를 넘겨줌 (비교 대상)
                        AddFileToListView(lvwRightDir, file, txtLeftDir.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 읽기 오류: " + ex.Message);
            }
            finally
            {
                lvwLeftDir.EndUpdate();
                lvwRightDir.EndUpdate();
            }
        }

        // 개별 파일을 리스트뷰 항목으로 만들고 색상을 결정하는 함수
        private void AddFileToListView(ListView lv, FileInfo file, string opponentPath)
        {
            ListViewItem item = new ListViewItem(file.Name);
            item.SubItems.Add((file.Length / 1024).ToString("N0") + " KB");
            item.SubItems.Add(file.LastWriteTime.ToString("yyyy-MM-dd tt hh:mm"));

            // 상대방 경로가 올바를 때만 비교 수행
            if (Directory.Exists(opponentPath))
            {
                string opponentFilePath = Path.Combine(opponentPath, file.Name);

                if (File.Exists(opponentFilePath))
                {
                    FileInfo opponentFile = new FileInfo(opponentFilePath);

                    // [수업 자료 35p 상태 정의]
                    if (file.LastWriteTime > opponentFile.LastWriteTime)
                        item.ForeColor = Color.Red;    // New (빨강)
                    else if (file.LastWriteTime < opponentFile.LastWriteTime)
                        item.ForeColor = Color.Gray;   // Old (회색)
                    else
                        item.ForeColor = Color.Black;  // 동일 (검정)
                }
                else
                {
                    item.ForeColor = Color.Purple;     // 단독 (보라)
                }
            }
            else
            {
                // 상대방 폴더가 아직 선택되지 않았다면 모두 기본 검은색으로 표시
                item.ForeColor = Color.Black;
            }

            lv.Items.Add(item);
        }

        // 왼쪽 폴더 선택 버튼
        private void btnLeftDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtLeftDir.Text = dlg.SelectedPath;
                    CompareAndDisplay(); // 경로 선택 시 즉시 비교
                }
            }
        }

        // 오른쪽 폴더 선택 버튼
        private void btnRightDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtRightDir.Text = dlg.SelectedPath;
                    CompareAndDisplay(); // 경로 선택 시 즉시 비교
                }
            }
        }

        // Designer wires this event; implement as no-op to avoid missing method compile error
        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            // Intentionally left blank. The designer expects this handler to exist.
        }

        // [과제 3 핵심] 파일 복사 실행 함수
        private void CopyFile(ListView sourceLv, string sourcePath, string targetPath)
        {
            // 1. 선택된 파일이 있는지 확인
            if (sourceLv.SelectedItems.Count == 0)
            {
                MessageBox.Show("복사할 파일을 선택해주세요.");
                return;
            }

            foreach (ListViewItem item in sourceLv.SelectedItems)
            {
                string fileName = item.Text;
                string sourceFile = Path.Combine(sourcePath, fileName);
                string targetFile = Path.Combine(targetPath, fileName);

                // 2. 대상 폴더에 이미 파일이 있는 경우 날짜 비교 및 확인 창 띄우기
                if (File.Exists(targetFile))
                {
                    FileInfo sInfo = new FileInfo(sourceFile);
                    FileInfo tInfo = new FileInfo(targetFile);

                    string msg = $"대상 폴더에 동일한 파일이 존재합니다. 덮어쓰시겠습니까?\n\n" +
                                 $"[보내는 파일]: {sInfo.LastWriteTime}\n" +
                                 $"[기존 파일]: {tInfo.LastWriteTime}";

                    if (MessageBox.Show(msg, "파일 복사 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        continue; // '아니오'를 누르면 다음 파일로 넘어감
                    }
                }

                try
                {
                    // 3. 파일 복사 수행 (true 설정 시 기존 파일 덮어쓰기 허용)
                    File.Copy(sourceFile, targetFile, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{fileName} 복사 중 오류 발생: {ex.Message}");
                }
            }

            // 4. 복사 완료 후 리스트 갱신 (색상 다시 계산)
            CompareAndDisplay();
        }

        // [>>>] 오른쪽 방향 버튼 클릭 시 (왼쪽 -> 오른쪽 복사)
        private void btnCopyFromRight_Click(object sender, EventArgs e)
        {
            CopyFile(lvwRightDir, txtRightDir.Text, txtLeftDir.Text);
        }

        // [<<<] 왼쪽 방향 버튼 클릭 시 (오른쪽 -> 왼쪽 복사)
        private void btnCopyFromLeft_Click(object sender, EventArgs e)
        {
            CopyFile(lvwLeftDir, txtLeftDir.Text, txtRightDir.Text);
        }
    }
}