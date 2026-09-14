using UnityEngine;

namespace CRClone.Core
{
    public static class UnityObjectExtensions
    {
        /// <summary>
        /// Bridges Unity's "fake null" to real null.
        /// <para>
        /// UnityEngine.Object overloads <c>operator ==</c> so that destroyed or
        /// unassigned references compare equal to null, but the null-conditional
        /// operator (<c>?.</c>) and null-coalescing (<c>??</c>) bypass that overload
        /// and test the managed reference instead. Writing <c>_button?.onClick</c> on
        /// an unassigned <c>[SerializeField]</c> therefore does NOT short-circuit --
        /// it dereferences the fake-null and throws
        /// <c>UnassignedReferenceException</c> / <c>MissingReferenceException</c>.
        /// </para>
        /// <para>
        /// <c>_button.OrNull()?.onClick</c> collapses the fake-null to a genuine null
        /// first, so the operator short-circuits as intended.
        /// </para>
        /// </summary>
        public static T OrNull<T>(this T obj) where T : Object
        {
            return obj == null ? null : obj;
        }
    }
}
