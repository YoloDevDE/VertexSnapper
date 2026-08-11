- Rotate the selection around the contact normal with the scroll wheel, in 15 degree steps.
- Give the edge mode a reference direction too, from the normal of the triangle next to it - then
  edge snapping would be fully determined as well.
- Fallback for the face snap: snap edge to edge, then put the editor gizmo on that shared edge so
  the angle can be turned by hand. Half the work automatic, the rest under the user's control.
- Solve the hinge angle by contact instead of by picking a face: swing the selection around the
  shared edge until its surfaces meet the target's, treating the faces as the collider. The angle
  is then found rather than described, and the second pick on each side falls away.
