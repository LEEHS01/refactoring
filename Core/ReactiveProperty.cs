using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HNS.Core
{
    /// <summary>
    /// ReactiveProperty - 값이 변경될 때 UnityEvent를 자동으로 호출하는 반응형 프로퍼티
    /// Inspector 기반 MVVM의 핵심 구성 요소
    /// </summary>
    [Serializable]
    public class ReactiveProperty<T>
    {
        [SerializeField] private T _value;
        private UnityEvent<T> _onValueChanged = new UnityEvent<T>();

        /// <summary>
        /// Inspector에서 연결할 UnityEvent
        /// </summary>
        public UnityEvent<T> OnValueChanged => _onValueChanged;

        /// <summary>
        /// 현재 값 - 설정 시 자동으로 이벤트 발생
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    var oldValue = _value;
                    _value = value;
                    _onValueChanged.Invoke(value);
                    OnValueChangedInternal?.Invoke(oldValue, value);
                }
            }
        }

        /// <summary>
        /// 내부용 이벤트 (코드에서 구독용)
        /// </summary>
        private event System.Action<T, T> OnValueChangedInternal;

        public ReactiveProperty()
        {
            _value = default(T);
        }

        public ReactiveProperty(T initialValue)
        {
            _value = initialValue;
        }

        /// <summary>
        /// 이벤트 발생 없이 값 설정 (초기화용)
        /// </summary>
        public void SetValueWithoutNotify(T value)
        {
            _value = value;
        }

        /// <summary>
        /// 코드에서 값 변경 구독
        /// </summary>
        public void Subscribe(System.Action<T> callback)
        {
            _onValueChanged.AddListener(new UnityAction<T>(callback));
        }

        /// <summary>
        /// 변경 전후 값을 모두 받는 구독
        /// </summary>
        public void SubscribeWithPrevious(System.Action<T, T> callback)
        {
            OnValueChangedInternal += callback;
        }

        /// <summary>
        /// 현재 값으로 즉시 이벤트 발생 (초기화용)
        /// </summary>
        public void NotifyCurrentValue()
        {
            _onValueChanged.Invoke(_value);
        }

        public static implicit operator T(ReactiveProperty<T> property)
        {
            return property.Value;
        }
    }

    /// <summary>
    /// ReactiveCollection - 리스트 변경 시 UnityEvent를 자동으로 호출하는 반응형 컬렉션
    /// </summary>
    [Serializable]
    public class ReactiveCollection<T> : System.Collections.Generic.List<T>
    {
        private UnityEvent<List<T>> _onCollectionChanged = new UnityEvent<List<T>>();
        private UnityEvent<T> _onItemAdded = new UnityEvent<T>();
        private UnityEvent<T> _onItemRemoved = new UnityEvent<T>();

        /// <summary>
        /// 컬렉션 변경 시 Inspector에서 연결할 UnityEvent
        /// </summary>
        public UnityEvent<List<T>> OnCollectionChanged => _onCollectionChanged;

        /// <summary>
        /// 아이템 추가 시 Inspector에서 연결할 UnityEvent
        /// </summary>
        public UnityEvent<T> OnItemAdded => _onItemAdded;

        /// <summary>
        /// 아이템 제거 시 Inspector에서 연결할 UnityEvent
        /// </summary>
        public UnityEvent<T> OnItemRemoved => _onItemRemoved;

        public new void Add(T item)
        {
            base.Add(item);
            _onItemAdded.Invoke(item);
            _onCollectionChanged.Invoke(this);
        }

        public new bool Remove(T item)
        {
            bool removed = base.Remove(item);
            if (removed)
            {
                _onItemRemoved.Invoke(item);
                _onCollectionChanged.Invoke(this);
            }
            return removed;
        }

        public new void Clear()
        {
            base.Clear();
            _onCollectionChanged.Invoke(this);
        }

        /// <summary>
        /// 이벤트 발생 없이 아이템들 설정 (대량 업데이트용)
        /// </summary>
        public void SetItemsWithoutNotify(System.Collections.Generic.IEnumerable<T> items)
        {
            base.Clear();
            base.AddRange(items);
        }

        /// <summary>
        /// 현재 컬렉션으로 즉시 이벤트 발생
        /// </summary>
        public void NotifyCurrentCollection()
        {
            _onCollectionChanged.Invoke(this);
        }

        /// <summary>
        /// 코드에서 컬렉션 변경 구독
        /// </summary>
        public void Subscribe(System.Action<List<T>> callback)
        {
            _onCollectionChanged.AddListener(new UnityAction<List<T>>(callback));
        }
    }
}