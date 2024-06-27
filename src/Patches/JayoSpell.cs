using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnhollowerBaseLib;
using UnityEngine;
using JayoVNyan;

namespace TunicRandomizer {
    public class JayoSpell : HealSpell {

        public static List<DPAD> CustomInputs = new List<DPAD>() { DPAD.UP, DPAD.UP, DPAD.UP, DPAD.DOWN, DPAD.DOWN, DPAD.DOWN };

        public JayoSpell(IntPtr ptr) : base(ptr) { }

        private void Awake() {
            base.inputsToCast = new UnhollowerBaseLib.Il2CppStructArray<DPAD>(1L);
            base.manaCost = 0;
            base.hpToGive = 0;
            base.particles = PlayerCharacter.instance.transform.GetChild(8).gameObject.GetComponent<ParticleSystem>();
        }

        public override bool CheckInput(Il2CppStructArray<DPAD> inputs, int length) {
            if (length == CustomInputs.Count) {
                for (int i = 0; i < length; i++) {
                    if (inputs[i] != CustomInputs[i]) {
                        return false;
                    }
                }
                DoSpell();
            }
            return false;
        }

        public void DoSpell()
        {
            base.SpellEffect();
            PlayerCharacter.instance.transform.localRotation = new Quaternion(0, 0.9239f, 0, -0.3827f);
            PlayerCharacter.instance.GetComponent<Animator>().SetBool("wave", true);
            Notifications.Show("[jayo] suhp nurdz?", "howd yawl geht sO hehki^ kyoot? [luckycup]");
            VNyanSender.SendActionToVNyan("TunicPlayerRads", new { status = "false", rad_amount = PlayerCharacter.GetRadiationShowAmount() });
        }

        public static void MagicSpell_CheckInput_PostfixPatch(MagicSpell __instance, Il2CppStructArray<DPAD> inputs, int length) {
            JayoSpell JayoSpell = __instance.TryCast<JayoSpell>();
            if (JayoSpell != null) {
                JayoSpell.CheckInput(inputs, length);
            }
        }
    }
}
