using GameSave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Model
{
    public abstract class SaveConfig : LoAConfig
    {
        /// <summary>
        /// 저장하려고 하는 데이터를 반환합니다. 에러가 발생시 세이브 데이터가 없는것으로 간주합니다.
        /// 리턴값은 실제로 해당 모드에 대응하는 별도의 패키지 아이디 키로 감싸져서 보관되므로 별도의 고유 키값을 가질 필요가 없습니다.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public abstract SaveData GetSaveData(LibraryModel instance);

        /// <summary>
        /// <see cref="GetSaveData(LibraryModel)"/> 에서 저장한 데이터가 있다면 세이브 로드 시점에 불러옵니다.
        /// </summary>
        /// <param name="data"></param>
        public abstract void LoadFromSaveData(SaveData data);
    }
}
