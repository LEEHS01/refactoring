namespace Core
{
    /// <summary>
    /// ViewModel �������̽�
    /// ��� ViewModel�� �ݵ�� �����ؾ� �ϴ� ���
    /// </summary>
    public interface IViewModel
    {
        /// <summary>
        /// �ʱ�ȭ ����
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// �ε� �� ����
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// ViewModel �ʱ�ȭ
        /// View���� ȣ���
        /// </summary>
        void Initialize();

        /// <summary>
        /// ViewModel ���� (�޸� ����, �̺�Ʈ ���� ���� ��)
        /// View�� �ı��� �� ȣ���
        /// </summary>
        void Cleanup();
    }
}